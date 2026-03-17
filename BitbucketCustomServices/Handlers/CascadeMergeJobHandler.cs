using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BitbucketCustomServices.Handlers;

public class CascadeMergeJobHandler : IWebhookJobHandler
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 5000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebhookJobChannel _channel;

    public CascadeMergeJobHandler(IServiceScopeFactory scopeFactory, IWebhookJobChannel channel)
    {
        _scopeFactory = scopeFactory;
        _channel = channel;
    }

    public bool CanHandle(WebhookJob job) =>
        job.Target is WebhookJobTarget.CascadeMerge &&
        job.EventType is EventType.PullRequestMerged &&
        !string.IsNullOrEmpty(job.PullRequestEvent.PullRequest?.Destination?.Branch?.Name) &&
        job.Repository != null && job.Repository.CascadeMergeEnabled;

    public async Task HandleAsync(WebhookJob job, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repositoryService = scope.ServiceProvider.GetRequiredService<IRepositoryService>();
        var cascadeMergeService = scope.ServiceProvider.GetRequiredService<ICascadeMergeService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CascadeMergeJobHandler>>();

        var repository = await repositoryService.GetRepositoryByWorkspaceAndSlug(job.Workspace, job.RepoSlug);
        if (repository == null)
        {
            logger.LogWarning("Repository not found for cascade merge: {Workspace}/{RepoSlug}", job.Workspace, job.RepoSlug);
            return;
        }

        var destBranch = job.PullRequestEvent.PullRequest.Destination.Branch.Name;
        var branchMappings = repository.BranchMappings
            .Where(x => x.From == destBranch)
            .ToList();

        if (branchMappings.Count == 0)
        {
            logger.LogInformation("No mapping for {Branch} in {Repo}", destBranch, job.RepoSlug);
            return;
        }

        if (!repositoryService.ValidateRepositoryCredentials(repository))
        {
            logger.LogWarning("Invalid credentials for repository {Repo}", job.RepoSlug);
            return;
        }

        var (successCount, failureCount) = await cascadeMergeService.ProcessCascadeMerge(
            repository, job.PullRequestEvent, job.Workspace, job.RepoSlug, destBranch);

        if (failureCount > 0 && job.RetryCount < MaxRetries)
        {
            logger.LogInformation("Re-queuing cascade merge job for retry {Retry}/{Max} after {Delay}ms: {Workspace}/{RepoSlug}",
                job.RetryCount + 1, MaxRetries, RetryDelayMs, job.Workspace, job.RepoSlug);
            await Task.Delay(RetryDelayMs, cancellationToken);
            var retryJob = job with { RetryCount = job.RetryCount + 1 };
            await _channel.WriteAsync(retryJob, cancellationToken);
        }
    }
}
