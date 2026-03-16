using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Handlers;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services;
using BitbucketCustomServices.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class WebhookJobProcessorTests
{
    private static WebhookJob CreateJob()
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
        return new WebhookJob("ws", "repo", evt, EventType.PullRequestMerged);
    }

    private class TestChannel : IWebhookJobChannel
    {
        private WebhookJob? _job;
        private bool _completed;

        public void SetJob(WebhookJob job)
        {
            _job = job;
        }

        public void Complete()
        {
            _completed = true;
        }

        public ValueTask WriteAsync(WebhookJob job, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(_job != null && !_completed);
        }

        public bool TryRead(out WebhookJob job)
        {
            if (_job != null)
            {
                job = _job;
                _job = null;
                return true;
            }
            job = null!;
            return false;
        }
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesJobWithMatchingHandler()
    {
        var job = CreateJob();
        var channel = new TestChannel();
        channel.SetJob(job);
        var handler = new Mock<IWebhookJobHandler>();
        handler.Setup(x => x.CanHandle(It.IsAny<WebhookJob>())).Returns<WebhookJob>(j => j.Workspace == job.Workspace);
        handler.Setup(x => x.HandleAsync(It.IsAny<WebhookJob>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var services = new ServiceCollection();
        services.AddSingleton(channel);
        services.AddSingleton<IWebhookJobHandler>(handler.Object);
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(x => x.ServiceProvider).Returns(sp);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(x => x.CreateScope()).Returns(mockScope.Object);
        var processor = new WebhookJobProcessor(channel, mockScopeFactory.Object, Mock.Of<ILogger<WebhookJobProcessor>>());
        var cts = new CancellationTokenSource();
        var runTask = processor.StartAsync(cts.Token);
        await Task.Delay(100);
        channel.Complete();
        cts.CancelAfter(200);
        try { await runTask; } catch (OperationCanceledException) { }
        handler.Verify(x => x.HandleAsync(It.IsAny<WebhookJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
