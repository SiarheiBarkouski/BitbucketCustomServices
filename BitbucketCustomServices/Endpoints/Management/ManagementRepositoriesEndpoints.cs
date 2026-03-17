using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Management;

public static class ManagementRepositoriesEndpoints
{
    public static WebApplication MapManagementRepositoriesEndpoints(this WebApplication app)
    {
        app.MapGet("/api/projects/{projectId:guid}/repositories", [Authorize] async (AppDbContext dbContext,
            [FromRoute] Guid projectId,
            HttpContext httpContext,
            UserManager<User> userManager) =>
        {
            var isAdminOrModerator = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Moderator");

            var query = dbContext.Repositories
                .Where(x => x.ProjectId == projectId);

            if (!isAdminOrModerator)
            {
                var userId = httpContext.User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                query = query.Where(r => r.UserToRepositoryAccesses.Any(a => a.UserId == userId));
            }

            var repositories = await query.ToListAsync();

            return Results.Ok(repositories);
        });

        app.MapGet("/api/repositories/{id:guid}", [Authorize] async (AppDbContext dbContext,
            Guid id,
            HttpContext httpContext,
            UserManager<User> userManager) =>
        {
            var repository = await dbContext.Repositories
                .Where(x => x.Id == id)
                .Include(x => x.RepositoryCredentials)
                .Include(x => x.BranchMappings.OrderBy(bm => bm.From))
                .Include(x => x.RepositoryNotificationSettings)
                .FirstOrDefaultAsync();

            if (repository == null)
                return Results.NotFound();

            var isAdminOrModerator = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Moderator");
            if (!isAdminOrModerator)
            {
                var userId = httpContext.User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var hasAccess = await dbContext.Repositories
                    .Where(r => r.Id == id)
                    .AnyAsync(r => r.UserToRepositoryAccesses.Any(a => a.UserId == userId));

                if (!hasAccess)
                {
                    return Results.Forbid();
                }
            }

            return Results.Ok(repository);
        });

        app.MapPost("/api/repositories", [Authorize(Roles = "Admin,Moderator")] async (AppDbContext dbContext,
            Repository repository,
            HttpContext httpContext,
            UserManager<User> userManager) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            repository.UserToRepositoryAccesses ??= [];

            repository.RepositoryNotificationSettings ??= new RepositoryNotificationSettings
            {
                EventType = Enums.EventType.Default
            };

            repository.UserToRepositoryAccesses.Add(new UserToRepositoryAccess
            {
                UserId = userId,
                RepositoryId = repository.Id
            });

            dbContext.Repositories.Add(repository);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/repositories/{repository.Id}", repository);
        });

        app.MapPut("/api/repositories/{id:guid}", [Authorize] async (AppDbContext dbContext,
            Guid id,
            Repository updatedRepository,
            HttpContext httpContext,
            UserManager<User> userManager) =>
        {
            var userId = httpContext.User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var isAdminOrModerator = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Moderator");
            if (!isAdminOrModerator)
            {
                var hasAccess = await dbContext.Repositories
                    .Where(x => x.Id == id)
                    .SelectMany(x => x.UserToRepositoryAccesses)
                    .AnyAsync(x => x.UserId == userId);

                if (!hasAccess)
                    return Results.Forbid();
            }

            var repository = await dbContext.Repositories
                .Where(x => x.Id == id)
                .Include(x => x.RepositoryCredentials)
                .Include(x => x.BranchMappings)
                .Include(x => x.RepositoryNotificationSettings)
                .FirstOrDefaultAsync();

            if (repository == null)
                return Results.NotFound();

            repository.Name = updatedRepository.Name;
            repository.MergeStrategy = updatedRepository.MergeStrategy;
            repository.CascadeMergeEnabled = updatedRepository.CascadeMergeEnabled;
            repository.TelegramNotificationsEnabled = updatedRepository.TelegramNotificationsEnabled;
            repository.TelegramBotToken = updatedRepository.TelegramBotToken;
            repository.TelegramChatId = updatedRepository.TelegramChatId;
            repository.WebhookSecret = updatedRepository.WebhookSecret;

            if (updatedRepository.RepositoryCredentials != null)
            {
                repository.RepositoryCredentials ??= new RepositoryCredentials
                {
                    RepositoryId = repository.Id
                };

                repository.RepositoryCredentials.AuthType = updatedRepository.RepositoryCredentials.AuthType;
                repository.RepositoryCredentials.Username = updatedRepository.RepositoryCredentials.Username;
                repository.RepositoryCredentials.Password = updatedRepository.RepositoryCredentials.Password;
                repository.RepositoryCredentials.Email = updatedRepository.RepositoryCredentials.Email;
                repository.RepositoryCredentials.Token = updatedRepository.RepositoryCredentials.Token;
            }

            if (updatedRepository.RepositoryNotificationSettings != null)
            {
                repository.RepositoryNotificationSettings ??= new RepositoryNotificationSettings
                {
                    RepositoryId = repository.Id
                };

                repository.RepositoryNotificationSettings.EventType = updatedRepository.RepositoryNotificationSettings.EventType;
                repository.RepositoryNotificationSettings.IgnoreAutoMergeNotifications = updatedRepository.RepositoryNotificationSettings.IgnoreAutoMergeNotifications;
            }

            if (updatedRepository.BranchMappings != null)
            {
                dbContext.BranchesMappings.RemoveRange(repository.BranchMappings);

                foreach (var mapping in updatedRepository.BranchMappings)
                {
                    mapping.RepositoryId = repository.Id;
                    dbContext.BranchesMappings.Add(mapping);
                }
            }

            await dbContext.SaveChangesAsync();

            return Results.Ok(repository);
        });

        app.MapDelete("/api/repositories/{id:guid}", [Authorize(Roles = "Admin,Moderator")] async (AppDbContext dbContext,
            Guid id,
            HttpContext httpContext,
            UserManager<User> userManager) =>
        {
            var repository = await dbContext.Repositories.FindAsync(id);

            if (repository == null)
                return Results.NotFound();

            dbContext.Repositories.Remove(repository);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}