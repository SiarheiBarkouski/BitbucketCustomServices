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

public class CascadeMergeJobHandlerTests
{
    private static readonly Changes EmptyChanges = new(new CommitChanges([]), new DiffChanges(null, null));

    private static WebhookJob CreateJob(EventType eventType, string? destBranchName = "main")
    {
        var destBranch = destBranchName != null ? new Branch(destBranchName) : null!;
        var dest = new Destination(
            new Models.Repository("ws/repo"),
            destBranch,
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var src = new Source(
            new Models.Repository("ws/repo"),
            new Branch("feature"),
            new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!)));
        var pr = new PullRequest(
            dest, src, new Author("a", "a", null!), "Title", "", 1, [], []);
        var evt = new PullRequestEvent(new Actor("a"), pr, EmptyChanges);
        return new WebhookJob("ws", "repo", evt, eventType);
    }

    [Fact]
    public void CanHandle_WhenPullRequestMergedAndDestBranchSet_ReturnsTrue()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, "main");
        Assert.True(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenEventTypeNotMerged_ReturnsFalse()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestCreated, "main");
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenDestBranchEmpty_ReturnsFalse()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, "");
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public void CanHandle_WhenDestBranchNull_ReturnsFalse()
    {
        var sp = new ServiceCollection().BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged, null);
        Assert.False(handler.CanHandle(job));
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryFound_ProcessesCascadeMerge()
    {
        var repo = new Entities.Repository
        {
            Name = "repo",
            MergeStrategy = "merge_commit",
            BranchMappings = [new BranchMapping { From = "main", To = "develop" }],
            RepositoryCredentials = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        repoService.Setup(x => x.ValidateRepositoryCredentials(repo)).Returns(true);
        var cascadeMerge = new Mock<ICascadeMergeService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => cascadeMerge.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        cascadeMerge.Verify(x => x.ProcessCascadeMerge(repo, job.PullRequestEvent, "ws", "repo", "main"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryNotFound_ReturnsWithoutProcessing()
    {
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync((Entities.Repository?)null);
        var cascadeMerge = new Mock<ICascadeMergeService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => cascadeMerge.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        cascadeMerge.Verify(x => x.ProcessCascadeMerge(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNoBranchMappings_ReturnsWithoutProcessing()
    {
        var repo = new Entities.Repository
        {
            Name = "repo",
            BranchMappings = [],
            RepositoryCredentials = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        var cascadeMerge = new Mock<ICascadeMergeService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => cascadeMerge.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        cascadeMerge.Verify(x => x.ProcessCascadeMerge(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenInvalidCredentials_ReturnsWithoutProcessing()
    {
        var repo = new Entities.Repository
        {
            Name = "repo",
            BranchMappings = [new BranchMapping { From = "main", To = "develop" }],
            RepositoryCredentials = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "" }
        };
        var repoService = new Mock<IRepositoryService>();
        repoService.Setup(x => x.GetRepositoryByWorkspaceAndSlug("ws", "repo")).ReturnsAsync(repo);
        repoService.Setup(x => x.ValidateRepositoryCredentials(repo)).Returns(false);
        var cascadeMerge = new Mock<ICascadeMergeService>();
        var services = new ServiceCollection();
        services.AddScoped(_ => repoService.Object);
        services.AddScoped(_ => cascadeMerge.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var handler = new CascadeMergeJobHandler(sp.GetRequiredService<IServiceScopeFactory>());
        var job = CreateJob(EventType.PullRequestMerged);

        await handler.HandleAsync(job);

        cascadeMerge.Verify(x => x.ProcessCascadeMerge(It.IsAny<Entities.Repository>(), It.IsAny<PullRequestEvent>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
