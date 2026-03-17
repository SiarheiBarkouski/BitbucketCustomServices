using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Handlers;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class TelegramNotificationJobHandlerTests
{
    private static WebhookJob CreateJob(EventType eventType, Entities.Repository? repository = null)
    {
        var dest = new Destination(
            new Models.Repository("ws/repo"),
            new Branch("main"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var src = new Source(
            new Models.Repository("ws/repo"),
            new Branch("feature"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var pr = new PullRequest(dest, src, new Author("a", "a", null!), "Title", "", 1, [], []);
        var evt = new PullRequestEvent(new Actor("a"), pr, new Changes(new CommitChanges([]), new DiffChanges(null, null)));
        return new WebhookJob("ws", "repo", evt, eventType, WebhookJobTarget.TelegramNotification, repository);
    }

    [Fact]
    public void CanHandle_WhenEventTypeNotDefaultAndTelegramEnabled_ReturnsTrue()
    {
        var repo = new Entities.Repository { TelegramNotificationsEnabled = true };
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, repo);
        Assert.True(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenRepositoryNull_ReturnsFalse()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, null);
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenEventTypeDefault_ReturnsFalse()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.Default);
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenTelegramNotificationsDisabled_ReturnsFalse()
    {
        var repo = new Entities.Repository { TelegramNotificationsEnabled = false };
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, repo);
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenTelegramNotificationsEnabled_ReturnsTrue()
    {
        var repo = new Entities.Repository { TelegramNotificationsEnabled = true };
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, repo);
        Assert.True(handler.CanHandle(job));
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryNotFound_ReturnsWithoutSending()
    {
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync((Entities.Repository?)null);
        var notification = new Mock<INotificationService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => notification.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        notification.Verify(x => x.SendTelegramNotification(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<EventType>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNotificationSettingsNull_ReturnsWithoutSending()
    {
        var repo = new Entities.Repository { Name = "repo", RepositoryNotificationSettings = null! };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        var notification = new Mock<INotificationService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => notification.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        notification.Verify(x => x.SendTelegramNotification(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<EventType>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenEventTypeNotEnabled_ReturnsWithoutSending()
    {
        var repo = new Entities.Repository
        {
            Name = "repo",
            RepositoryNotificationSettings = new RepositoryNotificationSettings
            {
                EventType = EventType.PullRequestCreated,
                IgnoreAutoMergeNotifications = false
            }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        var notification = new Mock<INotificationService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => notification.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        notification.Verify(x => x.SendTelegramNotification(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<EventType>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAutoMergeIgnoredAndTitleMatches_SkipsNotification()
    {
        var dest = new Destination(
            new Models.Repository("ws/repo"),
            new Branch("main"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var src = new Source(
            new Models.Repository("ws/repo"),
            new Branch("feature"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var pr = new PullRequest(dest, src, new Author("a", "a", null!), "Auto branch merge for PR #1 (Dev)", "", 1, [], []);
        var evt = new PullRequestEvent(new Actor("a"), pr, new Changes(new CommitChanges([]), new DiffChanges(null, null)));
        var job = new WebhookJob("ws", "repo", evt, EventType.PullRequestMerged, WebhookJobTarget.TelegramNotification);
        var repo = new Entities.Repository
        {
            Name = "repo",
            RepositoryNotificationSettings = new RepositoryNotificationSettings
            {
                EventType = EventType.PullRequestMerged,
                IgnoreAutoMergeNotifications = true
            }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        var notification = new Mock<INotificationService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => notification.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());

        await handler.HandleAsync(job);

        notification.Verify(x => x.SendTelegramNotification(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<EventType>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenAllConditionsMet_SendsNotification()
    {
        var repo = new Entities.Repository
        {
            Name = "repo",
            RepositoryNotificationSettings = new RepositoryNotificationSettings
            {
                EventType = EventType.PullRequestMerged,
                IgnoreAutoMergeNotifications = false
            }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        var notification = new Mock<INotificationService>();
        notification.Setup(x => x.SendTelegramNotification(repo, It.IsAny<PullRequestEvent>(), EventType.PullRequestMerged)).ReturnsAsync(true);
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => notification.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new TelegramNotificationJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        notification.Verify(x => x.SendTelegramNotification(repo, job.PullRequestEvent, EventType.PullRequestMerged), Times.Once);
    }
}
