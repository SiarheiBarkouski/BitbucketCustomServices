#nullable enable
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BitbucketCustomServices.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Management;

public static class ManagementUsersEndpoints
{
    private static readonly string[] BuiltInRoles = ["Admin", "Moderator", "User"];

    public static WebApplication MapManagementUsersEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users/all", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, UserManager<User> userManager) =>
            {
                var users = await dbContext.Users
                    .Include(u => u.UserToRepositoryAccesses)
                        .ThenInclude(a => a.Repository)
                            .ThenInclude(r => r.Project)
                    .ToListAsync();
                
                var userDtos = new List<object>();

                foreach (var user in users)
                {
                    var roles = await userManager.GetRolesAsync(user);
                    
                    var accessibleProjects = user.UserToRepositoryAccesses
                        .Select(a => new
                        {
                            ProjectId = a.Repository.Project.Id,
                            ProjectName = a.Repository.Project.Name,
                            RepositoryId = a.Repository.Id,
                            RepositoryName = a.Repository.Name
                        })
                        .GroupBy(x => new { x.ProjectId, x.ProjectName })
                        .Select(g => new
                        {
                            g.Key.ProjectId,
                            g.Key.ProjectName,
                            Repositories = g.Select(r => new
                            {
                                r.RepositoryId,
                                r.RepositoryName
                            }).ToList()
                        })
                        .ToList();
                    
                    userDtos.Add(new
                    {
                        user.Id,
                        user.UserName,
                        user.Email,
                        Roles = roles,
                        AccessibleProjects = accessibleProjects,
                        RepositoryCount = user.UserToRepositoryAccesses.Count
                    });
                }

                return Results.Ok(userDtos);
            });

        app.MapGet("/api/users/{id}", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, string id, UserManager<User> userManager) =>
            {
                var user = await dbContext.Users.FindAsync(id);
                if (user == null)
                    return Results.NotFound();

                var roles = await userManager.GetRolesAsync(user);
                var userDto = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    Roles = roles
                };

                return Results.Ok(userDto);
            });

        app.MapPost("/api/users", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, 
                [FromBody] CreateUserRequest request, 
                UserManager<User> userManager) =>
            {
                if (string.IsNullOrWhiteSpace(request.UserName) || 
                    string.IsNullOrWhiteSpace(request.Email) || 
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest("Username, email, and password are required");
                }

                if (request.Roles == null || request.Roles.Count == 0)
                {
                    return Results.BadRequest("User must have exactly one role");
                }

                if (request.Roles.Count > 1)
                {
                    return Results.BadRequest("User can only have one role");
                }

                if (!BuiltInRoles.Contains(request.Roles[0]))
                {
                    return Results.BadRequest($"Role must be one of: {string.Join(", ", BuiltInRoles)}");
                }

                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return Results.BadRequest("User with this email already exists");
                }

                existingUser = await userManager.FindByNameAsync(request.UserName);
                if (existingUser != null)
                {
                    return Results.BadRequest("User with this username already exists");
                }

                var user = new User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Results.BadRequest($"Failed to create user: {errors}");
                }

                await userManager.AddToRoleAsync(user, request.Roles[0]);

                var roles = await userManager.GetRolesAsync(user);
                var userDto = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    Roles = roles
                };

                return Results.Created($"/api/users/{user.Id}", userDto);
            });

        app.MapPut("/api/users/{id}", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, 
                string id, 
                [FromBody] UpdateUserRequest request, 
                UserManager<User> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                    return Results.NotFound();

                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    var existingUserWithEmail = await userManager.FindByEmailAsync(request.Email);
                    if (existingUserWithEmail != null && existingUserWithEmail.Id != user.Id)
                    {
                        return Results.BadRequest("User with this email already exists");
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.UserName)
                {
                    var existingUserWithUsername = await userManager.FindByNameAsync(request.UserName);
                    if (existingUserWithUsername != null && existingUserWithUsername.Id != user.Id)
                    {
                        return Results.BadRequest("User with this username already exists");
                    }
                }

                user.UserName = request.UserName ?? user.UserName;
                user.Email = request.Email ?? user.Email;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Results.BadRequest($"Failed to update user: {errors}");
                }

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await userManager.ResetPasswordAsync(user, token, request.Password);
                    if (!passwordResult.Succeeded)
                    {
                        var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                        return Results.BadRequest($"Failed to update password: {errors}");
                    }
                }

                bool rolesChanged = false;
                if (request.Roles != null)
                {
                    if (request.Roles.Count == 0)
                    {
                        return Results.BadRequest("User must have exactly one role");
                    }

                    if (request.Roles.Count > 1)
                    {
                        return Results.BadRequest("User can only have one role");
                    }

                    if (!BuiltInRoles.Contains(request.Roles[0]))
                    {
                        return Results.BadRequest($"Role must be one of: {string.Join(", ", BuiltInRoles)}");
                    }

                    var currentRoles = await userManager.GetRolesAsync(user);
                    
                    var rolesAreEqual = currentRoles.Count == request.Roles.Count && 
                                       currentRoles.All(r => request.Roles.Contains(r));
                    
                    if (!rolesAreEqual)
                    {
                        rolesChanged = true;
                        await userManager.RemoveFromRolesAsync(user, currentRoles);
                        
                        await userManager.AddToRoleAsync(user, request.Roles[0]);
                        
                        await userManager.UpdateSecurityStampAsync(user);
                    }
                }

                var roles = await userManager.GetRolesAsync(user);
                var userDto = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    Roles = roles,
                    ForcedLogout = rolesChanged
                };

                return Results.Ok(userDto);
            });

        app.MapDelete("/api/users/{id}", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, string id, UserManager<User> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user == null)
                    return Results.NotFound();

                var result = await userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Results.BadRequest($"Failed to delete user: {errors}");
                }

                return Results.NoContent();
            });

        app.MapGet("/api/roles", [Authorize(Roles = "Admin")]
            () =>
            {
                return Results.Ok(BuiltInRoles);
            });

        return app;
    }

    public record CreateUserRequest(
        string UserName,
        string Email,
        string Password,
        List<string>? Roles
    );

    public record UpdateUserRequest(
        string? UserName,
        string? Email,
        string? Password,
        List<string>? Roles
    );
}

