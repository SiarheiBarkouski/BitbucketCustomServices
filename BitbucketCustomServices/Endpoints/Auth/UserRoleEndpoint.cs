using Microsoft.AspNetCore.Identity;
using BitbucketCustomServices.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Auth;

public static class UserRoleEndpoint
{
    public static WebApplication MapUserRoleEndpoints(this WebApplication app)
    {
        app.MapGet("/api/user/isadmin", [Authorize]
            (AppDbContext dbContext, HttpContext httpContext, UserManager<User> userManager) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                return Results.Ok(new { isAdmin });
            });

        app.MapGet("/api/user/canmanage", [Authorize]
            (AppDbContext dbContext, HttpContext httpContext, UserManager<User> userManager) =>
            {
                var canManage = httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("Moderator");

                return Results.Ok(new { canManage });
            });

        return app;
    }
}