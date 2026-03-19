using System.Text.RegularExpressions;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;

namespace BitbucketCustomServices.Handlers;

public class ConflictBranchCleanupHandler : IWebhookJobHandler
{
    private static readonly Regex ConflictBranchPattern = new(
        @"^cascade\.merge/conflict-from-.+-to-.+-.+$",
        RegexOptions.Compiled);

    private readonly IServiceScopeFactory _scopeFactory;

    public ConflictBranchCleanupHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(WebhookJob job) =>
        job.Target is WebhookJobTarget.ConflictBranchCleanup &&
        job.EventType is EventType.PullRequestMerged &&
        job.Repository != null &&
        job.Repository.CascadeMergeEnabled &&
        job.PullRequestEvent.PullRequest?.Source?.Branch?.Name is { } sourceBranch &&
        ConflictBranchPattern.IsMatch(sourceBranch);

    public async Task HandleAsync(WebhookJob job, CancellationToken cancellationToken = default)
    {
        var sourceBranch = job.PullRequestEvent.PullRequest.Source.Branch.Name;
        using var scope = _scopeFactory.CreateScope();
        var bitbucketService = scope.ServiceProvider.GetRequiredService<IBitbucketService>();
        var repositoryService = scope.ServiceProvider.GetRequiredService<IRepositoryService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConflictBranchCleanupHandler>>();

        var repository = await repositoryService.GetRepositoryByWorkspaceAndSlug(job.Workspace, job.RepoSlug);
        if (repository == null)
        {
            logger.LogWarning("Repository not found for conflict branch cleanup: {Workspace}/{RepoSlug}", job.Workspace, job.RepoSlug);
            return;
        }

        if (!repositoryService.ValidateRepositoryCredentials(repository))
        {
            logger.LogWarning("Invalid credentials for repository {Repo}", job.RepoSlug);
            return;
        }

        using var httpClient = await bitbucketService.GetAuthenticatedClient(repository.RepositoryCredentials);
        var deleted = await bitbucketService.DeleteBranch(httpClient, job.Workspace, job.RepoSlug, sourceBranch);
        if (deleted)
            logger.LogInformation("Deleted conflict branch: {Branch}", sourceBranch);
        else
            logger.LogWarning("Failed to delete conflict branch: {Branch}", sourceBranch);
    }
}
