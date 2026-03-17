using BitbucketCustomServices.Constants;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Entities_Repository = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices.Services;

public class CascadeMergeService : ICascadeMergeService
{
    private readonly IBitbucketService _bitbucketService;
    private readonly ILogger<CascadeMergeService> _logger;
    private readonly INotificationService _notificationService;

    public CascadeMergeService(
        IBitbucketService bitbucketService,
        ILogger<CascadeMergeService> logger,
        INotificationService notificationService)
    {
        _bitbucketService = bitbucketService;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<(int SuccessCount, int FailureCount)> ProcessCascadeMerge(
        Entities_Repository repository, 
        PullRequestEvent prEvent, 
        string workspace, 
        string repoSlug, 
        string destBranch)
    {
        using var httpClient = await _bitbucketService.GetAuthenticatedClient(repository.RepositoryCredentials);

        var initialPrAuthor = prEvent.PullRequest.GetInitialPrAuthor() ?? 
                             prEvent.PullRequest.Author?.DisplayName ?? "Unknown";
        var initialPrId = prEvent.PullRequest.GetInitialPrId() ?? prEvent.PullRequest.Id;

        var branchMappings = repository.BranchMappings
            .Where(x => x.From == destBranch)
            .ToList();

        // Process all branch mappings with the same "From" field
        var successCount = 0;
        var failureCount = 0;
        
        foreach (var mapping in branchMappings)
        {
            var targetBranch = mapping.To;
            _logger.LogInformation("Processing branch mapping: {SourceBranch} → {TargetBranch}", 
                destBranch, targetBranch);
            
            try
            {
                // Create pull request
                var prTitle = string.Format(PullRequestConstants.AutoMergeTitle, PullRequestConstants.AutoMergeTitlePartToDetectIgnore, initialPrId, initialPrAuthor);
                var prDescription = string.Format(PullRequestConstants.AutoMergeDescription, initialPrId, initialPrAuthor);
                
                var createdPr = await _bitbucketService.CreatePullRequest(
                    httpClient, workspace, repoSlug, destBranch, targetBranch, prTitle, prDescription);

                // Try to merge the pull request
                var mergeMessage = string.Format(PullRequestConstants.AutoMergeMessage, 
                    destBranch, targetBranch, initialPrId, initialPrAuthor);
                
                var mergeSuccess = await _bitbucketService.MergePullRequest(
                    httpClient, workspace, repoSlug, createdPr.Id, mergeMessage, repository.MergeStrategy);

                if (!mergeSuccess)
                {
                    // Handle merge conflict by creating a conflict branch
                    var conflictBranch = string.Format(BranchConstants.ConflictBranchName, destBranch, targetBranch, initialPrId);
                    
                    var branchCreated = await _bitbucketService.CreateBranch(
                        httpClient, repository.RepositoryCredentials, workspace, repoSlug, conflictBranch, createdPr.Source.Commit.Hash);

                    if (branchCreated)
                    {
                        _logger.LogInformation("Created conflict branch: {Branch}", conflictBranch);

                        var conflictTitle = string.Format(PullRequestConstants.AutoMergeConflictTitle, initialPrId, initialPrAuthor);
                        
                        await _bitbucketService.UpdatePullRequest(httpClient, workspace, repoSlug, createdPr.Id, conflictBranch, conflictTitle);
                        
                        await _notificationService.SendTelegramNotification(repository, createdPr, initialPrAuthor, EventType.MergeConflict);
                        
                        successCount++;
                        _logger.LogInformation("Conflict handled for {SourceBranch} → {TargetBranch}", destBranch, targetBranch);
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning("Merge failed for {SourceBranch} → {TargetBranch}", destBranch, targetBranch);
                    }
                }
                else
                {
                    successCount++;
                    _logger.LogInformation("Successfully merged {SourceBranch} → {TargetBranch}", destBranch, targetBranch);
                }
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger.LogError(ex, "Error processing branch mapping {SourceBranch} → {TargetBranch}", 
                    destBranch, targetBranch);
            }
        }

        _logger.LogInformation("Branch mapping processing complete. Success: {SuccessCount}, Failures: {FailureCount}", 
            successCount, failureCount);
            
        return (successCount, failureCount);
    }
} 