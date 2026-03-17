using BitbucketCustomServices.Enums;
using RepositoryEntity = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices.Models;

public record WebhookJob(
    string Workspace,
    string RepoSlug,
    PullRequestEvent PullRequestEvent,
    EventType EventType,
    WebhookJobTarget Target,
    RepositoryEntity? Repository = null,
    int RetryCount = 0);
