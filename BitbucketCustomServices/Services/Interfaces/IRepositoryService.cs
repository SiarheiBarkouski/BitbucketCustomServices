using BitbucketCustomServices.Entities;

namespace BitbucketCustomServices.Services.Interfaces;

public interface IRepositoryService
{
    Task<Repository> GetRepositoryByWorkspaceAndSlug(string workspace, string repoSlug);

    bool ValidateRepositoryCredentials(Repository repository);
}