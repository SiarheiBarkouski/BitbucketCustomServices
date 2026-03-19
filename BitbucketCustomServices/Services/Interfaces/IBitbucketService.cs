using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Services.Interfaces;

public interface IBitbucketService
{
    Task<HttpClient> GetAuthenticatedClient(RepositoryCredentials credentials);

    Task<PullRequest> CreatePullRequest(HttpClient client,
        string workspace,
        string repoSlug,
        string sourceBranch,
        string targetBranch,
        string title,
        string description);

    Task<bool> MergePullRequest(HttpClient client,
        string workspace,
        string repoSlug,
        int pullRequestId,
        string message,
        string mergeStrategy);

    Task<bool> CreateBranch(HttpClient client,
        RepositoryCredentials credentials,
        string workspace,
        string repoSlug,
        string branchName,
        string commitHash);

    Task<bool> UpdatePullRequest(HttpClient client,
        string workspace,
        string repoSlug,
        int pullRequestId,
        string sourceBranch,
        string title);

    Task<bool> DeleteBranch(HttpClient client,
        string workspace,
        string repoSlug,
        string branchName);
}