using BitbucketCustomServices.Models;
using Entities_Repository = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices.Services.Interfaces;

public interface ICascadeMergeService
{
    Task<(int SuccessCount, int FailureCount)> ProcessCascadeMerge(
        Entities_Repository repository, 
        PullRequestEvent prEvent, 
        string workspace, 
        string repoSlug, 
        string destBranch);
}