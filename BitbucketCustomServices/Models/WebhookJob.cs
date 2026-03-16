using BitbucketCustomServices.Enums;

namespace BitbucketCustomServices.Models;

public record WebhookJob(
    string Workspace,
    string RepoSlug,
    PullRequestEvent PullRequestEvent,
    EventType EventType);
