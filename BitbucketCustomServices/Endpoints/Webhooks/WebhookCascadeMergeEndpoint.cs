using System.Net;
using System.Text.Json;
using BitbucketCustomServices.Extensions;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace BitbucketCustomServices.Endpoints.Webhooks;

public static class WebhookCascadeMergeEndpoint
{
    public static WebApplication MapWebhookCascadeMergeEndpoint(this WebApplication app)
    {
        app.MapPost("/webhook/cascade_merge", [AllowAnonymous] async (context) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var repositoryService = context.RequestServices.GetRequiredService<IRepositoryService>();
            var cascadeMergeService = context.RequestServices.GetRequiredService<ICascadeMergeService>();

            try
            {
                var prEvent = await JsonSerializer.DeserializeAsync<PullRequestEvent>(context.Request.Body);
                if (prEvent?.PullRequest?.Destination is not
                    {
                        Branch: { Name: var destBranch },
                        Repository: { FullName: var repoFullName }
                    })
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return;
                }
                
                var eventType = context.Request.GetEventType();

                if (eventType is not EventType.PullRequestMerged)
                {
                    logger.LogError("Invalid event type received for PR #{PullRequestId}, EventType: {EventType}",
                        prEvent.PullRequest.Id, context.Request.Headers["X-Event-Key"]);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }

                var repoParts = repoFullName.Split('/');
                if (repoParts.Length != 2)
                {
                    logger.LogError("Invalid repository format: {RepoFullName}", repoFullName);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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

                var branchMappings = repository.BranchMappings
                    .Where(x => x.From == destBranch)
                    .ToList();
                
                if (branchMappings.Count == 0)
                {
                    logger.LogInformation("No mapping for {Branch} in {Repo}", destBranch, repoSlug);
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    return;
                }

                if (!repositoryService.ValidateRepositoryCredentials(repository))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return;
                }

                var (successCount, failureCount) = await cascadeMergeService.ProcessCascadeMerge(
                    repository, prEvent, workspace, repoSlug, destBranch);

                // Return 200 OK if at least one mapping was processed successfully
                if (successCount > 0)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                else if (failureCount > 0)
                {
                    // Return 500 if all attempts failed
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                else
                {
                    // Should not happen but just in case
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
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