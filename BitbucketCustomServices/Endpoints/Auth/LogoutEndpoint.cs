using BitbucketCustomServices.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BitbucketCustomServices.Endpoints.Auth;

public static class LogoutEndpoint
{
    public static WebApplication MapLogoutEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/logout", [Authorize] async (HttpContext context, SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();

            foreach (var cookie in context.Request.Cookies.Keys)
            {
                context.Response.Cookies.Delete(cookie);
            }

            return Results.Ok(new { success = true, message = "Logged out successfully" });
        });

        return app;
    }
}