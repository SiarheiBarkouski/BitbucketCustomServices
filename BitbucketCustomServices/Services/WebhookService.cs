using System.Text.Json;
using BitbucketCustomServices.Extensions;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;

namespace BitbucketCustomServices.Services;

public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(ILogger<WebhookService> logger)
    {
        _logger = logger;
    }

    public async Task<(PullRequestEvent Event, EventType EventType, string Workspace, string RepoSlug, string DestBranch)?> 
        ParseWebhookPayload(Stream requestBody, HttpRequest request)
    {
        try
        {
            var prEvent = await JsonSerializer.DeserializeAsync<PullRequestEvent>(requestBody);
            if (prEvent?.PullRequest?.Destination is not
                {
                    Branch: { Name: var destBranch },
                    Repository: { FullName: var repoFullName }
                })
            {
                return null;
            }
            
            var eventType = request.GetEventType();
            
            var repoParts = repoFullName.Split('/');
            if (repoParts.Length != 2)
            {
                _logger.LogError("Invalid repository format: {RepoFullName}", repoFullName);
                return null;
            }

            var (workspace, repoSlug) = (repoParts[0], repoParts[1]);
            
            return (prEvent, eventType, workspace, repoSlug, destBranch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing webhook payload");
            return null;
        }
    }
} 