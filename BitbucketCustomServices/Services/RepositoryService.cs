using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BitbucketCustomServices.Services;

public class RepositoryService : IRepositoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RepositoryService> _logger;

    public RepositoryService(AppDbContext dbContext,
        ILogger<RepositoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Repository> GetRepositoryByWorkspaceAndSlug(string workspace,
        string repoSlug)
    {
        return await _dbContext.Projects
            .Where(x => x.Name == workspace)
            .SelectMany(x => x.Repositories)
            .Where(r => r.Name == repoSlug)
            .Include(x => x.BranchMappings)
            .Include(x => x.RepositoryCredentials)
            .Include(x => x.RepositoryNotificationSettings)
            .SingleOrDefaultAsync();
    }

    public bool ValidateRepositoryCredentials(Repository repository)
    {
        if (repository.RepositoryCredentials is null)
        {
            _logger.LogWarning("No credentials found for repository: {Repo}", repository.Name);

            return false;
        }

        var (isValid, errorMessage) = repository.RepositoryCredentials.Validate();

        if (isValid)
            return true;

        _logger.LogInformation("Invalid credentials setup for repository {Repo}. Error: {ErrorMessage}",
            repository.Name, errorMessage);

        return false;
    }
}