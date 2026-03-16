#nullable enable
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserEntity = BitbucketCustomServices.Entities.User;
using RepositoryEntity = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices;

public static class SeedData
{
    private const string AdminRole = "Admin";
    private const string ModeratorRole = "Moderator";
    private const string UserRole = "User";
    private const string DefaultAdminEmail = "admin@example.com";
    private const string DefaultAdminUsername = "admin";
    private const string DefaultAdminPassword = "Admin!123";

    private static readonly string[] _standardRoles = SeedConfigHelper.StandardRoles;

    private static EventType DefaultEventTypes => SeedConfigHelper.DefaultEventTypes;

    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<UserEntity> userManager,
        AppDbContext dbContext,
        IConfiguration configuration)
    {
        var seedConfig = LoadSeedConfiguration(configuration);

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(roleManager, userManager, seedConfig);

        if (seedConfig != null)
        {
            await SeedProjectsAsync(dbContext, userManager, seedConfig);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        await EnsureRolesExistAsync(roleManager, _standardRoles);
    }

    private static async Task EnsureRolesExistAsync(
        RoleManager<IdentityRole> roleManager,
        string[] roleNames)
    {
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    private static async Task SeedUsersAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<UserEntity> userManager,
        SeedConfiguration? seedConfig)
    {
        var hasAdminUser = seedConfig?.Users?.Any(u =>
                                                      !string.IsNullOrEmpty(u.Role) &&
                                                      u.Role.Equals(AdminRole, StringComparison.OrdinalIgnoreCase)) ??
                           false;

        if (!hasAdminUser)
        {
            await CreateDefaultAdminUserAsync(userManager);
        }

        if (seedConfig?.Users is not { Count: > 0 })
            return;

        foreach (var userConfig in seedConfig.Users)
        {
            if (!IsValidUserConfig(userConfig))
                continue;

            var existingUser = await userManager.FindByEmailAsync(userConfig.Email);

            if (existingUser != null)
                continue;

            await CreateUserAsync(roleManager, userManager, userConfig);
        }
    }

    private static async Task CreateDefaultAdminUserAsync(UserManager<UserEntity> userManager)
    {
        var existingAdmin = await userManager.FindByEmailAsync(DefaultAdminEmail);

        if (existingAdmin != null)
            return;

        var admin = new UserEntity
        {
            UserName = DefaultAdminUsername,
            Email = DefaultAdminEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(admin, DefaultAdminPassword);
        await userManager.AddToRoleAsync(admin, AdminRole);
    }

    private static async Task CreateUserAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<UserEntity> userManager,
        SeedUserWithRoleConfiguration userConfig)
    {
        if (!IsValidRole(userConfig.Role))
            return;

        var user = new UserEntity
        {
            UserName = userConfig.UserName,
            Email = userConfig.Email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, userConfig.Password);

        if (!createResult.Succeeded)
            return;

        await userManager.AddToRoleAsync(user, userConfig.Role!);
    }

    private static async Task SeedProjectsAsync(
        AppDbContext dbContext,
        UserManager<UserEntity> userManager,
        SeedConfiguration seedConfig)
    {
        if (seedConfig.Projects is not { Count: > 0 })
            return;

        foreach (var projectConfig in seedConfig.Projects)
        {
            if (await dbContext.Projects.AnyAsync(x => x.Name == projectConfig.Name))
                continue;

            var repositories = projectConfig.Repositories
                .Select(SeedConfigHelper.CreateRepositoryFromConfig)
                .ToList();

            var project = new Project
            {
                Name = projectConfig.Name,
                Repositories = repositories
            };

            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync();

            await AssignUserAccessToRepositoriesAsync(dbContext, userManager, projectConfig, projectConfig.Name);
        }
    }

    private static async Task AssignUserAccessToRepositoriesAsync(
        AppDbContext dbContext,
        UserManager<UserEntity> userManager,
        SeedProjectConfiguration projectConfig,
        string projectName)
    {
        var repositories = await dbContext.Repositories
            .Where(r => r.Project.Name == projectName)
            .ToListAsync();

        var repositoryMap = repositories.ToDictionary(r => r.Name, r => r);

        var allUserNames = projectConfig.Repositories
            .Where(r => r.UserNames is { Count: > 0 })
            .SelectMany(r => r.UserNames!)
            .Distinct()
            .ToList();

        if (allUserNames.Count == 0)
            return;

        var users = await userManager.Users
            .Where(u => allUserNames.Contains(u.UserName!))
            .ToListAsync();

        var userMap = users.ToDictionary(u => u.UserName!, u => u);

        var accessToAdd = new List<UserToRepositoryAccess>();

        foreach (var repoConfig in projectConfig.Repositories)
        {
            if (repoConfig.UserNames is not { Count: > 0 })
                continue;

            if (!repositoryMap.TryGetValue(repoConfig.Name, out var repository))
                continue;

            foreach (var userName in repoConfig.UserNames)
            {
                if (!userMap.TryGetValue(userName, out var user))
                    continue;

                var accessExists = await dbContext.Set<UserToRepositoryAccess>()
                    .AnyAsync(a => a.UserId == user.Id && a.RepositoryId == repository.Id);

                if (!accessExists)
                {
                    accessToAdd.Add(new UserToRepositoryAccess
                    {
                        UserId = user.Id,
                        RepositoryId = repository.Id
                    });
                }
            }
        }

        if (accessToAdd.Count > 0)
        {
            dbContext.Set<UserToRepositoryAccess>().AddRange(accessToAdd);
            await dbContext.SaveChangesAsync();
        }
    }

    private static AuthType ResolveAuthType(SeedRepositoryConfiguration repoConfig) =>
        SeedConfigHelper.ResolveAuthType(repoConfig);

    private static bool IsValidUserConfig(SeedUserWithRoleConfiguration userConfig) =>
        SeedConfigHelper.IsValidUserConfig(userConfig);

    private static bool IsValidRole(string? role) =>
        SeedConfigHelper.IsValidRole(role);

    private static SeedConfiguration? LoadSeedConfiguration(IConfiguration configuration)
    {
        var seedConfig = configuration.Get<SeedConfiguration>();

        if (seedConfig == null)
            return null;

        var hasUsers = seedConfig.Users is { Count: > 0 };
        var hasProjects = seedConfig.Projects is { Count: > 0 };

        return hasUsers || hasProjects ? seedConfig : null;
    }
}