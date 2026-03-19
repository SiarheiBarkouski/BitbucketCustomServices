using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Handlers;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class ConflictBranchCleanupHandlerTests
{
    private static WebhookJob CreateJob(string sourceBranchName, Entities.Repository? repository = null)
    {
        var dest = new Destination(
            new Models.Repository("ws/repo"),
            new Branch("develop"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var src = new Source(
            new Models.Repository("ws/repo"),
            new Branch(sourceBranchName),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var pr = new PullRequest(dest, src, new Author("a", "a", null!), "Title", "", 1, [], []);
        var evt = new PullRequestEvent(new Actor("a"), pr, new Changes(new CommitChanges([]), new DiffChanges(null, null)));
        return new WebhookJob("ws", "repo", evt, EventType.PullRequestMerged, WebhookJobTarget.ConflictBranchCleanup, repository);
    }

    [Fact]
    public void CanHandle_WhenConflictBranchAndCascadeEnabled_ReturnsTrue()
    {
        var repo = new Entities.Repository { CascadeMergeEnabled = true };
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new ConflictBranchCleanupHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob("cascade.merge/conflict-from-main-to-develop-123", repo);
        Assert.True(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenRepositoryNull_ReturnsFalse()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new ConflictBranchCleanupHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob("cascade.merge/conflict-from-main-to-develop-123", null);
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenSourceBranchNotConflictPattern_ReturnsFalse()
    {
        var repo = new Entities.Repository { CascadeMergeEnabled = true };
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new ConflictBranchCleanupHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob("feature/my-branch", repo);
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenEventTypeNotMerged_ReturnsFalse()
    {
        var repo = new Entities.Repository { CascadeMergeEnabled = true };
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new ConflictBranchCleanupHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob("cascade.merge/conflict-from-main-to-develop-123", repo)
            with { EventType = EventType.PullRequestCreated };
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public async Task HandleAsync_WhenValid_DeletesBranch()
    {
        var repo = new Entities.Repository
        {
            CascadeMergeEnabled = true,
            RepositoryCredentials = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        repoService.Setup(x => x.ValidateRepositoryCredentials(repo)).Returns(true);
        var bitbucket = new Mock<IBitbucketService>();
        var client = new HttpClient();
        bitbucket.Setup(x => x.GetAuthenticatedClient(It.IsAny<RepositoryCredentials>())).ReturnsAsync(client);
        bitbucket.Setup(x => x.DeleteBranch(It.IsAny<HttpClient>(), "ws", "repo", "cascade.merge/conflict-from-main-to-develop-123"))
            .ReturnsAsync(true);
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => bitbucket.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new ConflictBranchCleanupHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob("cascade.merge/conflict-from-main-to-develop-123", repo);

        await handler.HandleAsync(job);

        bitbucket.Verify(x => x.DeleteBranch(It.IsAny<HttpClient>(), "ws", "repo", "cascade.merge/conflict-from-main-to-develop-123"), Times.Once);
    }
}
