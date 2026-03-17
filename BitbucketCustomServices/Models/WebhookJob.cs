using BitbucketCustomServices.Enums;
using RepositoryEntity = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices.Models;

public record WebhookJob(
    string Workspace,
    string RepoSlug,
    PullRequestEvent PullRequestEvent,
    EventType EventType,
    RepositoryEntity? Repository = null);
