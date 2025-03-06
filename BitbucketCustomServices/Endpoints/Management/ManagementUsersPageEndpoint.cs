using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Management;

public static class ManagementUsersPageEndpoint
{
    public static WebApplication MapManagementUsersPageEndpoint(this WebApplication app)
    {
        app.MapGet("/users", [Authorize(Roles = "Admin")] async (context) =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "users.html");
            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Page not found");
                return;
            }

            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(filePath);
        });

        return app;
    }
}

