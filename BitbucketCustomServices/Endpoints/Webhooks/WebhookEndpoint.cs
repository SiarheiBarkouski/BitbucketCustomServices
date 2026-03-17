using System.Net;
using System.Text.Json;
using BitbucketCustomServices.Extensions;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Webhooks;

public static class WebhookEndpoint
{
    /// <summary>
    /// Single entry point for all Bitbucket webhooks. One URL per repository.
    /// Verifies X-Hub-Signature if WebhookSecret is configured, then enqueues job for background processing.
    /// </summary>
    public static WebApplication MapWebhookEndpoint(this WebApplication app)
    {
        app.MapPost("/webhook", [AllowAnonymous] async (context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var repositoryService = context.RequestServices.GetRequiredService<IRepositoryService>();
            var channel = context.RequestServices.GetRequiredService<IWebhookJobChannel>();

            try
            {
                byte[] rawBody;
                await using (var ms = new MemoryStream())
                {
                    await context.Request.Body.CopyToAsync(ms);
                    rawBody = ms.ToArray();
                }

                var prEvent = JsonSerializer.Deserialize<PullRequestEvent>(rawBody);
                if (prEvent?.PullRequest?.Destination?.Repository?.FullName is not { } repoFullName)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }

                var repoParts = repoFullName.Split('/');
                if (repoParts.Length != 2)
                {
                    logger.LogError("Invalid repository format: {RepoFullName}", repoFullName);
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

                if (!WebhookSignatureVerifier.Verify(repository.WebhookSecret, rawBody,
                    context.Request.Headers["X-Hub-Signature"]))
                {
                    logger.LogWarning("Webhook signature verification failed for {Workspace}/{RepoSlug}", workspace, repoSlug);
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }

                var eventType = context.Request.GetEventType();
                if (eventType is EventType.Default)
                {
                    logger.LogInformation("Unhandled event type for PR #{PullRequestId}, X-Event-Key: {EventKey}",
                        prEvent.PullRequest.Id, context.Request.Headers["X-Event-Key"]);
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }

                var telegramJob = new WebhookJob(workspace, repoSlug, prEvent, eventType, Enums.WebhookJobTarget.TelegramNotification, repository);
                await channel.WriteAsync(telegramJob);
                
                var cascadeJob = new WebhookJob(workspace, repoSlug, prEvent, eventType, Enums.WebhookJobTarget.CascadeMerge, repository);
                await channel.WriteAsync(cascadeJob);

                logger.LogDebug("Webhook jobs enqueued for {Workspace}/{RepoSlug}, EventType: {EventType}",
                    workspace, repoSlug, eventType);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Webhook processing error");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        });

        return app;
    }
}
