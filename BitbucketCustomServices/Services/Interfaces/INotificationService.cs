using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using Entities_Repository = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices.Services.Interfaces;

public interface INotificationService
{
    Task<bool> SendTelegramNotification(
        Entities_Repository repository,
        PullRequestEvent webhookPayload,
        EventType eventType);

    Task<bool> SendTelegramNotification(
        Entities_Repository repository,
        PullRequest pullRequest,
        string actor,
        EventType eventType);
}