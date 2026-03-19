using BitbucketCustomServices.Constants;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;

namespace BitbucketCustomServices.Handlers;

public class TelegramNotificationJobHandler : IWebhookJobHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramNotificationJobHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(WebhookJob job) =>
        job.Target is WebhookJobTarget.TelegramNotification &&
        job.EventType is not EventType.Default &&
        job.Repository != null && job.Repository.TelegramNotificationsEnabled;

    public async Task HandleAsync(WebhookJob job, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repositoryService = scope.ServiceProvider.GetRequiredService<IRepositoryService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TelegramNotificationJobHandler>>();

        var repository = await repositoryService.GetRepositoryByWorkspaceAndSlug(job.Workspace, job.RepoSlug);
        if (repository?.RepositoryNotificationSettings == null)
        {
            logger.LogDebug("Repository or notification settings not found: {Workspace}/{RepoSlug}", job.Workspace, job.RepoSlug);
            return;
        }

        if (!repository.RepositoryNotificationSettings.EventType.HasFlag(job.EventType))
        {
            logger.LogDebug("Event type {EventType} not enabled for {Repo}", job.EventType, job.RepoSlug);
            return;
        }

        var pr = job.PullRequestEvent.PullRequest;
        if (repository.RepositoryNotificationSettings.IgnoreAutoMergeNotifications &&
            pr.Title.StartsWith(PullRequestConstants.AutoMergeTitlePartToDetectIgnore, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Skipping notification for auto-merge PR #{PrId}", pr.Id);
            return;
        }

        await notificationService.SendTelegramNotification(repository, job.PullRequestEvent, job.EventType);
    }
}
