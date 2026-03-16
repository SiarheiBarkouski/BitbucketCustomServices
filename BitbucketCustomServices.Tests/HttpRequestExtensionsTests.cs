using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class HttpRequestExtensionsTests
{
    [Fact]
    public void GetEventType_WhenHeaderMissing_ReturnsDefault()
    {
        var context = new DefaultHttpContext();
        Assert.Equal(EventType.Default, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsPullRequestFulfilled_ReturnsPullRequestMerged()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "pullrequest:fulfilled";
        Assert.Equal(EventType.PullRequestMerged, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsPullRequestCreated_ReturnsPullRequestCreated()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "pullrequest:created";
        Assert.Equal(EventType.PullRequestCreated, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsUnknown_ReturnsDefault()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "unknown:event";
        Assert.Equal(EventType.Default, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsPullRequestApproved_ReturnsPullRequestApproved()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "pullrequest:approved";
        Assert.Equal(EventType.PullRequestApproved, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsPullRequestRejected_ReturnsPullRequestDeclined()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "pullrequest:rejected";
        Assert.Equal(EventType.PullRequestDeclined, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsPullRequestChangesRequestCreated_ReturnsPullRequestChangesRequested()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "pullrequest:changes_request_created";
        Assert.Equal(EventType.PullRequestChangesRequested, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsPullRequestUpdated_ReturnsPullRequestUpdated()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "pullrequest:updated";
        Assert.Equal(EventType.PullRequestUpdated, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderHasWhitespace_TrimsAndParses()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "  pullrequest:fulfilled  ";
        Assert.Equal(EventType.PullRequestMerged, context.Request.GetEventType());
    }

    [Fact]
    public void GetEventType_WhenHeaderIsMergeConflict_ReturnsMergeConflict()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Event-Key"] = "MergeConflict";
        Assert.Equal(EventType.MergeConflict, context.Request.GetEventType());
    }
}
