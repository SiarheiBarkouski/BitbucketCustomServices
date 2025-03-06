using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Management;

public static class ManagementEndpoints
{
    public static WebApplication MapManagementEndpoints(this WebApplication app)
    {
        app.MapGet("/management", [Authorize] async (context) =>
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "management.html");
            if (!File.Exists(filePath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsync("Page not found");
                return;
            }

            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(filePath);
        });

        app.MapManagementProjectsEndpoints();
        app.MapManagementRepositoriesEndpoints();
        app.MapRepositoryAccessEndpoints();
        
        return app;
    }
}