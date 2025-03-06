using System.Net;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Config;

public static class ConfigEditorEndpoint
{
    public static WebApplication MapConfigEditorEndpoint(this WebApplication app)
    {
        app.MapGet("/config/edit", [Authorize(Roles = "Admin")] async (context) =>
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var currentConfig = await File.ReadAllTextAsync(configPath);

            var templatePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "config-editor.html");
            if (!File.Exists(templatePath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync("Editor template not found");

                return;
            }

            var encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            var htmlContent = await File.ReadAllTextAsync(templatePath);
            htmlContent = htmlContent.Replace("{{CONFIG}}", encoder.Encode(currentConfig));

            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(htmlContent);
        });

        return app;
    }
}