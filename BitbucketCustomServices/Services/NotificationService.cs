using System.Text;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Entities_Repository = BitbucketCustomServices.Entities.Repository;

namespace BitbucketCustomServices.Services;

public class NotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHttpClientFactory httpClientFactory, ILogger<NotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task<bool> SendTelegramNotification(
        Entities_Repository repository,
        PullRequestEvent pullRequestEvent,
        EventType eventType)
    {
        var actor = pullRequestEvent.Actor?.DisplayName ?? "Unknown";
        return SendTelegramNotification(repository, pullRequestEvent.PullRequest, actor, eventType);
    }
    
    public async Task<bool> SendTelegramNotification(
        Entities_Repository repository, 
        PullRequest pullRequest,
        string actor,
        EventType eventType)
    {
        if (string.IsNullOrWhiteSpace(repository.TelegramChatId) ||
            string.IsNullOrWhiteSpace(repository.TelegramBotToken))
        {
            _logger.LogInformation("""
                                  Telegram notification skipped for PR #{PullRequestId}, EventType: {EventType}
                                  because bot token / chat id is missing.
                                  """, pullRequest.Id, eventType);
            
            return false;
        }

        var pr = pullRequest;
        var repositoryFullName = pr.Destination.Repository.FullName;
        var prTitle = pr.Title;
        var prUrl = $"https://bitbucket.org/{repositoryFullName}/pull-requests/{pr.Id}";
        var repositoryUrl = $"https://bitbucket.org/{repositoryFullName}";
        var destinationBranch = pr.Destination.Branch.Name;
        var sourceBranch = pr.Source.Branch.Name;
        var reviewers = pr.Reviewers?.Select(r => r.DisplayName).ToList() ?? new List<string>();

        var eventTypeMessageHeader = eventType switch
        {
            EventType.PullRequestCreated => $"🅿️ New Pull Request by {actor} 🅿️",
            EventType.PullRequestChangesRequested => $"🔄 Changes Required by {actor} 🔄",
            EventType.PullRequestDeclined => $"❌ Pull Request Declined by {actor} ❌",
            EventType.PullRequestMerged => $"✅ Pull Request Merged by {actor} ✅",
            EventType.PullRequestApproved when !pr.IsFullyApproved() => $"👍 Pull Request Approved by {actor} 👍",
            EventType.PullRequestApproved when pr.IsFullyApproved() => "🏁 Pull Request Approved by all reviewers 🏁",
            EventType.MergeConflict => $"🚨 Merge Conflict for {actor} 🚨",
            _ => "🔔 Pull Request Update"
        };

        var message = new StringBuilder();
        message.AppendLine($"<b>{eventTypeMessageHeader}</b>");
        message.AppendLine($"in <a href=\"{repositoryUrl}\">{repositoryFullName}</a>");
        message.AppendLine();
        message.AppendLine($"<b>Title:</b> <a href=\"{prUrl}\">{prTitle}</a>");
        if (reviewers.Count != 0)
        {
            message.AppendLine($"<b>Reviewers:</b> {string.Join(", ", reviewers)}");
        }
        message.AppendLine($"<b>Destination:</b> {sourceBranch} <b>=></b> {destinationBranch}");

        var telegramApiUrl = $"https://api.telegram.org/bot{repository.TelegramBotToken}/sendMessage";
        var telegramPayload = new
        {
            chat_id = repository.TelegramChatId,
            text = message.ToString(),
            parse_mode = "html"
        };

        using var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(telegramApiUrl, telegramPayload);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send Telegram notification: {Reason}", response.ReasonPhrase);
            return false;
        }

        return true;
    }
} 