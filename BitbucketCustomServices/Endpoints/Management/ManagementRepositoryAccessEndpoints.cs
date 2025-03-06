using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BitbucketCustomServices.Endpoints.Management;

public static class ManagementRepositoryAccessEndpoints
{
    public static WebApplication MapRepositoryAccessEndpoints(this WebApplication app)
    {
        app.MapGet("/api/repositories/{repositoryId:guid}/access", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, Guid repositoryId, HttpContext httpContext, UserManager<User> userManager) =>
            {
                var usersWithAccess = await dbContext.Users
                    .Where(u =>
                        u.UserToRepositoryAccesses.Any(a => a.RepositoryId == repositoryId) ||
                        u.UserRepositories.Any(r => r.Id == repositoryId)
                    )
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email
                    })
                    .ToListAsync();

                return Results.Ok(usersWithAccess);
            });

        app.MapPost("/api/repositories/{repositoryId:guid}/access", [Authorize(Roles = "Admin")] async (
            AppDbContext dbContext, Guid repositoryId, [FromBody] UserAccessRequest request,
            HttpContext httpContext, UserManager<User> userManager) =>
        {
            var repository = await dbContext.Repositories.FindAsync(repositoryId);
            if (repository == null)
            {
                return Results.NotFound("Repository not found");
            }

            var targetUser = await userManager.FindByIdAsync(request.UserId);
            if (targetUser == null)
            {
                return Results.NotFound("User not found");
            }

            var roles = await userManager.GetRolesAsync(targetUser);
            if (roles.Contains("Admin"))
            {
                return Results.BadRequest("Admin role has automatic access to all repositories");
            }

            var accessExists = await dbContext.Set<UserToRepositoryAccess>()
                .AnyAsync(a => a.UserId == request.UserId && a.RepositoryId == repositoryId);

            if (accessExists)
            {
                return Results.BadRequest("User already has access to this repository");
            }

            var access = new UserToRepositoryAccess
            {
                UserId = request.UserId,
                RepositoryId = repositoryId
            };

            dbContext.Add(access);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/repositories/{repositoryId}/access", access);
        });

        app.MapDelete("/api/repositories/{repositoryId:guid}/access/{userId}", [Authorize(Roles = "Admin")] async (
            AppDbContext dbContext, Guid repositoryId, string userId, HttpContext httpContext,
            UserManager<User> userManager) =>
        {
            var access = await dbContext.Set<UserToRepositoryAccess>()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.RepositoryId == repositoryId);

            if (access == null)
            {
                return Results.NotFound();
            }

            dbContext.Remove(access);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        app.MapGet("/api/users", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, HttpContext httpContext, UserManager<User> userManager) =>
            {
                var users = await dbContext.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email
                    })
                    .ToListAsync();

                return Results.Ok(users);
            });

        app.MapGet("/api/repositories/all", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, HttpContext httpContext, UserManager<User> userManager) =>
            {
                try
                {
                    var repositories = await dbContext.Repositories
                        .Include(r => r.Project)
                        .ToListAsync();

                    var result = repositories.Select(r => new
                    {
                        Id = r.Id,
                        Name = r.Name ?? "",
                        ProjectId = r.ProjectId,
                        ProjectName = r.Project?.Name ?? "Unknown Project"
                    })
                    .OrderBy(r => r.ProjectName)
                    .ThenBy(r => r.Name)
                    .ToList();

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error loading repositories: {ex.Message}", statusCode: 500);
                }
            });

        app.MapGet("/api/users/{userId}/repositories", [Authorize(Roles = "Admin")]
            async (AppDbContext dbContext, string userId, HttpContext httpContext, UserManager<User> userManager) =>
            {
                try
                {
                    var accesses = await dbContext.Set<UserToRepositoryAccess>()
                        .Where(a => a.UserId == userId)
                        .Include(a => a.Repository)
                            .ThenInclude(r => r.Project)
                        .ToListAsync();

                    var result = accesses.Select(a => new
                    {
                        Id = a.Repository.Id,
                        Name = a.Repository.Name ?? "",
                        ProjectId = a.Repository.ProjectId,
                        ProjectName = a.Repository.Project?.Name ?? "Unknown Project"
                    })
                    .ToList();

                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Error loading user repositories: {ex.Message}", statusCode: 500);
                }
            });

        return app;
    }
}
