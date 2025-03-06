using Microsoft.EntityFrameworkCore;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BitbucketCustomServices.Endpoints.Management;

public static class ManagementProjectsEndpoints
{
    public static WebApplication MapManagementProjectsEndpoints(this WebApplication app)
    {
        app.MapGet("/api/projects", [Authorize] 
            async (AppDbContext dbContext, HttpContext httpContext, UserManager<User> userManager) =>
            {
                var isAdminOrModerator = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Moderator");

                if (isAdminOrModerator)
                {
                    return Results.Ok((object)await dbContext.Projects.ToListAsync());
                }
                var userId = httpContext.User.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var projects = await dbContext.Projects
                    .Where(p => p.Repositories.Any(r => r.UserToRepositoryAccesses.Any(a => a.UserId == userId)))
                    .ToListAsync();

                return Results.Ok((object)projects);
            });

        app.MapGet("/api/projects/{id}", [Authorize] 
            async (AppDbContext dbContext, Guid id, HttpContext httpContext) => 
            Results.Ok((object)await dbContext.Projects.FindAsync(id)));

        app.MapPost("/api/projects", [Authorize(Roles = "Admin,Moderator")]
            async (AppDbContext dbContext, Project project, HttpContext httpContext, UserManager<User> userManager) =>
            {
                dbContext.Projects.Add(project);
                await dbContext.SaveChangesAsync();
                return Results.Created($"/api/projects/{project.Id}", project);
            });

        app.MapPut("/api/projects/{id}", [Authorize(Roles = "Admin,Moderator")]
            async (AppDbContext dbContext, Guid id, Project updatedProject, HttpContext httpContext,
                UserManager<User> userManager) =>
            {
                var project = await dbContext.Projects.FindAsync(id);

                if (project == null)
                    return Results.NotFound();

                project.Name = updatedProject.Name;
                await dbContext.SaveChangesAsync();
                return Results.NoContent();
            });

        app.MapDelete("/api/projects/{id}", [Authorize(Roles = "Admin,Moderator")]
            async (AppDbContext dbContext, Guid id, HttpContext httpContext, UserManager<User> userManager) =>
            {
                var project = await dbContext.Projects.FindAsync(id);

                if (project == null)
                    return Results.NotFound();

                dbContext.Projects.Remove(project);
                await dbContext.SaveChangesAsync();
                return Results.NoContent();
            });

        return app;
    }
}