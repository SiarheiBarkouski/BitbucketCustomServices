using System.Net.Http;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class CascadeMergeServiceTests
{
    private static PullRequestEvent CreatePullRequestEvent(int prId = 1, string destBranch = "main")
    {
        var dest = new Destination(
            new Models.Repository("ws/repo"),
            new Branch(destBranch),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", new Models.User("u1", "a"))));
        var src = new Source(
            new Models.Repository("ws/repo"),
            new Branch("feature"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", new Models.User("u1", "a"))));
        var pr = new PullRequest(dest, src, new Author("Dev", "dev@x.com", new Models.User("u1", "Dev")),
            "Title", "Desc", prId, [], []);
        return new PullRequestEvent(new Actor("Dev"), pr, new Changes(new CommitChanges([]), new DiffChanges(null, null)));
    }

    private static Entities.Repository CreateRepository(string destBranch, string targetBranch)
    {
        return new Entities.Repository
        {
            MergeStrategy = "merge_commit",
            BranchMappings = [new BranchMapping { From = destBranch, To = targetBranch }],
            RepositoryCredentials = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" }
        };
    }

    [Fact]
    public async Task ProcessCascadeMerge_WhenMergeSucceeds_ReturnsSuccessCount()
    {
        var pr = new PullRequest(
            new Destination(new Models.Repository("ws/repo"), new Branch("main"), new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", new Models.User("u1", "a")))),
            new Source(new Models.Repository("ws/repo"), new Branch("feature"), new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", new Models.User("u1", "a")))),
            new Author("Dev", "x", new Models.User("u1", "Dev")),
            "Title", "Desc", 99, [], []);
        var handler = new TestHelpers.MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("pullrequests") == true, System.Net.HttpStatusCode.OK, System.Text.Json.JsonSerializer.Serialize(pr));
        handler.Setup(req => req.RequestUri?.ToString().Contains("merge") == true, System.Net.HttpStatusCode.OK);
        var client = new HttpClient(handler);
        var bitbucket = new Mock<IBitbucketService>();
        bitbucket.Setup(x => x.GetAuthenticatedClient(It.IsAny<RepositoryCredentials>())).ReturnsAsync(client);
        bitbucket.Setup(x => x.CreatePullRequest(It.IsAny<HttpClient>(), "ws", "repo", "main", "develop", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(pr);
        bitbucket.Setup(x => x.MergePullRequest(It.IsAny<HttpClient>(), "ws", "repo", 99, It.IsAny<string>(), "merge_commit"))
            .ReturnsAsync(true);
        var notification = new Mock<INotificationService>();
        var logger = new Mock<ILogger<CascadeMergeService>>();
        var service = new CascadeMergeService(bitbucket.Object, logger.Object, notification.Object);
        var repo = CreateRepository("main", "develop");
        var evt = CreatePullRequestEvent(99);

        var (success, failure) = await service.ProcessCascadeMerge(repo, evt, "ws", "repo", "main");

        Assert.Equal(1, success);
        Assert.Equal(0, failure);
    }

    [Fact]
    public async Task ProcessCascadeMerge_WhenMergeFails_CreatesConflictBranchAndReturnsFailure()
    {
        var pr = new PullRequest(
            new Destination(new Models.Repository("ws/repo"), new Branch("main"), new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", new Models.User("u1", "a")))),
            new Source(new Models.Repository("ws/repo"), new Branch("feature"), new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", new Models.User("u1", "a")))),
            new Author("Dev", "x", new Models.User("u1", "Dev")),
            "Title", "Desc", 99, [], []);
        var handler = new TestHelpers.MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("merge") == true, System.Net.HttpStatusCode.Conflict);
        handler.Setup(req => req.RequestUri?.ToString().Contains("refs/branches") == true, System.Net.HttpStatusCode.Created);
        handler.Setup(req => req.RequestUri?.ToString().Contains("pullrequests") == true && req.Method == HttpMethod.Put, System.Net.HttpStatusCode.OK);
        var client = new HttpClient(handler);
        var bitbucket = new Mock<IBitbucketService>();
        bitbucket.Setup(x => x.GetAuthenticatedClient(It.IsAny<RepositoryCredentials>())).ReturnsAsync(client);
        bitbucket.Setup(x => x.CreatePullRequest(It.IsAny<HttpClient>(), "ws", "repo", "main", "develop", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(pr);
        bitbucket.Setup(x => x.MergePullRequest(It.IsAny<HttpClient>(), "ws", "repo", 99, It.IsAny<string>(), "merge_commit"))
            .ReturnsAsync(false);
        bitbucket.Setup(x => x.CreateBranch(It.IsAny<HttpClient>(), It.IsAny<RepositoryCredentials>(), "ws", "repo", It.IsAny<string>(), "h"))
            .ReturnsAsync(true);
        bitbucket.Setup(x => x.UpdatePullRequest(It.IsAny<HttpClient>(), "ws", "repo", 99, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        var notification = new Mock<INotificationService>();
        notification.Setup(x => x.SendTelegramNotification(It.IsAny<Entities.Repository>(), It.IsAny<PullRequest>(), It.IsAny<string>(), EventType.MergeConflict))
            .ReturnsAsync(true);
        var logger = new Mock<ILogger<CascadeMergeService>>();
        var service = new CascadeMergeService(bitbucket.Object, logger.Object, notification.Object);
        var repo = CreateRepository("main", "develop");
        var evt = CreatePullRequestEvent(99);

        var (success, failure) = await service.ProcessCascadeMerge(repo, evt, "ws", "repo", "main");

        Assert.Equal(0, success);
        Assert.Equal(1, failure);
    }

    [Fact]
    public async Task ProcessCascadeMerge_WhenNoBranchMappings_ReturnsZeroCounts()
    {
        var bitbucket = new Mock<IBitbucketService>();
        var notification = new Mock<INotificationService>();
        var logger = new Mock<ILogger<CascadeMergeService>>();
        var service = new CascadeMergeService(bitbucket.Object, logger.Object, notification.Object);
        var repo = CreateRepository("other", "develop");
        var evt = CreatePullRequestEvent();

        var (success, failure) = await service.ProcessCascadeMerge(repo, evt, "ws", "repo", "main");

        Assert.Equal(0, success);
        Assert.Equal(0, failure);
        bitbucket.Verify(x => x.CreatePullRequest(It.IsAny<HttpClient>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessCascadeMerge_WhenCreatePullRequestThrows_CountsFailure()
    {
        var bitbucket = new Mock<IBitbucketService>();
        var client = new HttpClient();
        bitbucket.Setup(x => x.GetAuthenticatedClient(It.IsAny<RepositoryCredentials>())).ReturnsAsync(client);
        bitbucket.Setup(x => x.CreatePullRequest(It.IsAny<HttpClient>(), "ws", "repo", "main", "develop", It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("API error"));
        var notification = new Mock<INotificationService>();
        var logger = new Mock<ILogger<CascadeMergeService>>();
        var service = new CascadeMergeService(bitbucket.Object, logger.Object, notification.Object);
        var repo = CreateRepository("main", "develop");
        var evt = CreatePullRequestEvent();

        var (success, failure) = await service.ProcessCascadeMerge(repo, evt, "ws", "repo", "main");

        Assert.Equal(0, success);
        Assert.Equal(1, failure);
    }
}
