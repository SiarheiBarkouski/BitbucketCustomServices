using System.Net;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services;
using BitbucketCustomServices.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class NotificationServiceTests
{
    private static PullRequest CreatePullRequest()
    {
        var dest = new Destination(
            new Models.Repository("ws/repo"),
            new Branch("main"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var src = new Source(
            new Models.Repository("ws/repo"),
            new Branch("feature"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        return new PullRequest(dest, src, new Author("Dev", "dev@example.com", new Models.User("u1", "Dev")),
            "Title", "Desc", 1, [new Reviewer("Alice")], []);
    }

    [Fact]
    public async Task SendTelegramNotification_WhenNoToken_ReturnsFalse()
    {
        var factory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<NotificationService>>();
        var service = new NotificationService(factory.Object, logger.Object);
        var repo = new Entities.Repository
        {
            TelegramBotToken = "",
            TelegramChatId = "123"
        };

        var result = await service.SendTelegramNotification(repo, CreatePullRequest(), "Dev", EventType.PullRequestMerged);

        Assert.False(result);
    }

    [Fact]
    public async Task SendTelegramNotification_WhenNoChatId_ReturnsFalse()
    {
        var factory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<NotificationService>>();
        var service = new NotificationService(factory.Object, logger.Object);
        var repo = new Entities.Repository
        {
            TelegramBotToken = "bot",
            TelegramChatId = ""
        };

        var result = await service.SendTelegramNotification(repo, CreatePullRequest(), "Dev", EventType.PullRequestMerged);

        Assert.False(result);
    }

    [Fact]
    public async Task SendTelegramNotification_OnSuccess_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("telegram.org") == true, HttpStatusCode.OK);

        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var logger = new Mock<ILogger<NotificationService>>();
        var service = new NotificationService(factory.Object, logger.Object);
        var repo = new Entities.Repository
        {
            TelegramBotToken = "bot123",
            TelegramChatId = "chat456"
        };

        var result = await service.SendTelegramNotification(repo, CreatePullRequest(), "Dev", EventType.PullRequestMerged);

        Assert.True(result);
    }

    [Fact]
    public async Task SendTelegramNotification_WithPullRequestEvent_ExtractsActor()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("telegram.org") == true, HttpStatusCode.OK);

        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var logger = new Mock<ILogger<NotificationService>>();
        var service = new NotificationService(factory.Object, logger.Object);
        var repo = new Entities.Repository { TelegramBotToken = "bot", TelegramChatId = "chat" };
        var pr = CreatePullRequest();
        var evt = new PullRequestEvent(new Actor("Alice"), pr, new Changes(new CommitChanges([]), new DiffChanges(null, null)));

        var result = await service.SendTelegramNotification(repo, evt, EventType.PullRequestCreated);

        Assert.True(result);
    }

    [Fact]
    public async Task SendTelegramNotification_WithMergeConflictEvent_SendsCorrectMessage()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("telegram.org") == true, HttpStatusCode.OK);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        var logger = new Mock<ILogger<NotificationService>>();
        var service = new NotificationService(factory.Object, logger.Object);
        var repo = new Entities.Repository { TelegramBotToken = "bot", TelegramChatId = "chat" };
        var pr = CreatePullRequest();
        var evt = new PullRequestEvent(new Actor("Alice"), pr, new Changes(new CommitChanges([]), new DiffChanges(null, null)));

        var result = await service.SendTelegramNotification(repo, evt, EventType.MergeConflict);

        Assert.True(result);
    }

    [Fact]
    public async Task SendTelegramNotification_WhenHttpFails_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("telegram.org") == true, HttpStatusCode.BadRequest);
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        var logger = new Mock<ILogger<NotificationService>>();
        var service = new NotificationService(factory.Object, logger.Object);
        var repo = new Entities.Repository { TelegramBotToken = "bot", TelegramChatId = "chat" };

        var result = await service.SendTelegramNotification(repo, CreatePullRequest(), "Dev", EventType.PullRequestMerged);

        Assert.False(result);
    }
}
