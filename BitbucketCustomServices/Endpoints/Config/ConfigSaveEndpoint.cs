using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Config;

public static class ConfigSaveEndpoint
{
    public static WebApplication MapConfigSaveEndpoint(this WebApplication app)
    {
        app.MapPost("/config/save", [Authorize(Roles = "Admin")] async (context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var configRoot = context.RequestServices.GetRequiredService<IConfigurationRoot>();
            var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var newConfigJson = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(newConfigJson))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync("Empty configuration payload");

                    return;
                }

                var validationResult = ValidateConfiguration(newConfigJson, logger);
                if (!validationResult.IsValid)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync($"Invalid configuration: {validationResult.ErrorMessage}");

                    return;
                }

                await CreateConfigurationBackup(configPath);
                await File.WriteAllTextAsync(configPath, newConfigJson);
                configRoot.Reload();

                logger.LogInformation("Configuration updated successfully");
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON validation failed");
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"JSON parse error: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving configuration");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsync($"Internal server error: {ex.Message}");
            }
        });

        static (bool IsValid, string ErrorMessage) ValidateConfiguration(string json, ILogger logger)
        {
            try
            {
                using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                return (true, string.Empty);
            }
            catch (JsonException ex)
            {
                logger.LogDebug("JSON validation error: {Message}", ex.Message);

                return (false, ex.Message);
            }
        }

        return app;
    }
    
    private static async Task CreateConfigurationBackup(string configPath)
    {
        var backupPath = $"{configPath}.{DateTime.UtcNow:yyyyMMddHHmmss}.bak";
        await using var sourceStream = File.Open(configPath, FileMode.Open);
        await using var backupStream = File.Create(backupPath);
        await sourceStream.CopyToAsync(backupStream);
    }
}