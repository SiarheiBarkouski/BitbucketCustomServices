using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Services.Interfaces;

public interface IWebhookService
{
    Task<(PullRequestEvent Event, EventType EventType, string Workspace, string RepoSlug, string DestBranch)?> 
        ParseWebhookPayload(Stream requestBody, HttpRequest request);
}