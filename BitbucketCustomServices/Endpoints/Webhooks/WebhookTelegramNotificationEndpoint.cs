using System.Net;
using System.Text.Json;
using BitbucketCustomServices.Constants;
using BitbucketCustomServices.Extensions;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Webhooks;

public static class WebhookTelegramNotificationEndpoint
{
    public static WebApplication MapWebhookTelegramNotificationEndpoint(this WebApplication app)
    {
        app.MapPost("/webhook/notification", [AllowAnonymous] async (context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var repositoryService = context.RequestServices.GetRequiredService<IRepositoryService>();
            var notificationService = context.RequestServices.GetRequiredService<INotificationService>();

            try
            {
                var webhookPayload = await JsonSerializer.DeserializeAsync<PullRequestEvent>(context.Request.Body);
                if (webhookPayload?.PullRequest == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var eventType = context.Request.GetEventType();

                if (eventType is EventType.Default)
                {
                    logger.LogError("Invalid event type received for PR #{PullRequestId}, EventType: {EventType}",
                        webhookPayload.PullRequest.Id, context.Request.Headers["X-Event-Key"]);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }

                var pr = webhookPayload.PullRequest;
                var repositoryFullName = pr.Destination.Repository.FullName;

                var repoParts = repositoryFullName.Split('/');
                if (repoParts.Length != 2)
                {
                    logger.LogError("Invalid repository format: {Repository}", repositoryFullName);
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var (workspace, repoSlug) = (repoParts[0], repoParts[1]);
                
                var repository = await repositoryService.GetRepositoryByWorkspaceAndSlug(workspace, repoSlug);
                if (repository == null)
                {
                    logger.LogError("Repository not found: {Workspace}/{RepoSlug}", workspace, repoSlug);
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
                
                if (!repository.RepositoryNotificationSettings.EventType.HasFlag(eventType))
                {
                    logger.LogInformation("Webhook telegram notification processing skipped for PR #{PullRequestId}, EventType: {EventType}",
                        webhookPayload.PullRequest.Id, eventType);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }

                if (repository.RepositoryNotificationSettings.IgnoreAutoMergeNotifications && 
                    pr.Title.StartsWith(PullRequestConstants.AutoMergeTitlePartToDetectIgnore, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogInformation("""
                                          Webhook telegram notification processing skipped for PR #{PullRequestId}, Title: {Title}
                                          because IgnoreAutoMergeNotifications is enabled.
                                          """, webhookPayload.PullRequest.Id, pr.Title);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }

                var success = await notificationService.SendTelegramNotification(repository, webhookPayload, eventType);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing webhook notification");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        });

        return app;
    }
}