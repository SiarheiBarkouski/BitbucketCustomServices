using System.Text;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class WebhookServiceTests
{
    private static readonly string ValidPayload = """
        {
          "actor": { "display_name": "Dev", "raw": "Dev <dev@x.com>", "user": { "uuid": "u1", "display_name": "Dev" } },
          "pullrequest": {
            "destination": {
              "repository": { "full_name": "ws/repo" },
              "branch": { "name": "main" },
              "commit": { "hash": "h", "date": "2024-01-01T00:00:00Z", "message": "m", "author": { "display_name": "a", "raw": "a", "user": { "uuid": "u1", "display_name": "a" } } }
            },
            "source": {
              "repository": { "full_name": "ws/repo" },
              "branch": { "name": "feature" },
              "commit": { "hash": "h", "date": "2024-01-01T00:00:00Z", "message": "m", "author": { "display_name": "a", "raw": "a", "user": { "uuid": "u1", "display_name": "a" } } }
            },
            "author": { "display_name": "Dev", "raw": "Dev <dev@x.com>", "user": { "uuid": "u1", "display_name": "Dev" } },
            "title": "Title",
            "description": "Desc",
            "id": 1,
            "reviewers": [],
            "participants": []
          },
          "changes": { "commits": { "added": [] }, "diff": { "added": null, "removed": null } }
        }
        """;

    private static HttpContext CreateHttpContext(string? eventKey = "pullrequest:fulfilled")
    {
        var context = new DefaultHttpContext();
        if (eventKey != null)
            context.Request.Headers["X-Event-Key"] = eventKey;
        return context;
    }

    [Fact]
    public async Task ParseWebhookPayload_WithValidPayload_ReturnsParsed()
    {
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(logger.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(ValidPayload));
        var context = CreateHttpContext();

        var result = await service.ParseWebhookPayload(stream, context.Request);

        Assert.NotNull(result);
        var (evt, eventType, workspace, repoSlug, destBranch) = result.Value;
        Assert.Equal(EventType.PullRequestMerged, eventType);
        Assert.Equal("ws", workspace);
        Assert.Equal("repo", repoSlug);
        Assert.Equal("main", destBranch);
        Assert.Equal(1, evt.PullRequest.Id);
    }

    [Fact]
    public async Task ParseWebhookPayload_WithInvalidJson_ReturnsNull()
    {
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(logger.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ invalid json }"));
        var context = CreateHttpContext();

        var result = await service.ParseWebhookPayload(stream, context.Request);

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseWebhookPayload_WithInvalidRepoFormat_ReturnsNull()
    {
        var payload = ValidPayload.Replace("\"full_name\": \"ws/repo\"", "\"full_name\": \"invalid-repo\"");
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(logger.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var context = CreateHttpContext();

        var result = await service.ParseWebhookPayload(stream, context.Request);

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseWebhookPayload_WithMissingDestination_ReturnsNull()
    {
        var payload = """
            {
              "actor": { "display_name": "Dev", "raw": "x", "user": { "uuid": "u1", "display_name": "Dev" } },
              "pullrequest": {
                "destination": null,
                "source": { "repository": { "full_name": "ws/repo" }, "branch": { "name": "f" }, "commit": { "hash": "h", "date": "2024-01-01T00:00:00Z", "message": "m", "author": { "display_name": "a", "raw": "a", "user": { "uuid": "u1", "display_name": "a" } } } },
                "author": { "display_name": "Dev", "raw": "x", "user": { "uuid": "u1", "display_name": "Dev" } },
                "title": "T", "description": "D", "id": 1, "reviewers": [], "participants": []
              },
              "changes": { "commits": { "added": [] }, "diff": { "added": null, "removed": null } }
            }
            """;
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(logger.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var context = CreateHttpContext();

        var result = await service.ParseWebhookPayload(stream, context.Request);

        Assert.Null(result);
    }

    [Fact]
    public async Task ParseWebhookPayload_WithMissingPullRequest_ReturnsNull()
    {
        var payload = """
            {
              "actor": { "display_name": "Dev", "raw": "x", "user": { "uuid": "u1", "display_name": "Dev" } },
              "pullrequest": null,
              "changes": { "commits": { "added": [] }, "diff": { "added": null, "removed": null } }
            }
            """;
        var logger = new Mock<ILogger<WebhookService>>();
        var service = new WebhookService(logger.Object);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        var context = CreateHttpContext();

        var result = await service.ParseWebhookPayload(stream, context.Request);

        Assert.Null(result);
    }
}
