using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BitbucketCustomServices.Handlers;

public class CascadeMergeJobHandler : IWebhookJobHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CascadeMergeJobHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(WebhookJob job) =>
        job.EventType is EventType.PullRequestMerged &&
        !string.IsNullOrEmpty(job.PullRequestEvent.PullRequest?.Destination?.Branch?.Name);

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

        await cascadeMergeService.ProcessCascadeMerge(
            repository, job.PullRequestEvent, job.Workspace, job.RepoSlug, destBranch);
    }
}
