using System.Net;
using System.Text.Json;
using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using BitbucketCustomServices.Services;
using BitbucketCustomServices.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class BitbucketServiceHttpTests
{
    private static BitbucketService CreateService(HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        var logger = new Mock<ILogger<BitbucketService>>();
        return new BitbucketService(factory.Object, logger.Object);
    }

    [Fact]
    public async Task CreatePullRequest_OnSuccess_ReturnsPullRequest()
    {
        var pr = new PullRequest(
            new Destination(new Models.Repository("ws/repo"), new Branch("main"), new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!))),
            new Source(new Models.Repository("ws/repo"), new Branch("feature"), new Commit("h", DateTime.UtcNow, "m", new Author("a", "a", null!))),
            new Author("Dev", "dev@example.com", new Models.User("u1", "Dev")),
            "Title", "Desc", 42, [], []);
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("pullrequests") == true,
            HttpStatusCode.OK,
            JsonSerializer.Serialize(pr));

        var client = new HttpClient(handler);
        var service = CreateService(client);
        var creds = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" };

        var result = await service.CreatePullRequest(client, "ws", "repo", "feature", "main", "Title", "Desc");

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
    }

    [Fact]
    public async Task CreatePullRequest_OnFailure_Throws()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("pullrequests") == true, HttpStatusCode.BadRequest);

        var client = new HttpClient(handler);
        var service = CreateService(client);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.CreatePullRequest(client, "ws", "repo", "feature", "main", "Title", "Desc"));
    }

    [Fact]
    public async Task MergePullRequest_OnSuccess_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("merge") == true, HttpStatusCode.OK);

        var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.MergePullRequest(client, "ws", "repo", 1, "msg", "merge_commit");

        Assert.True(result);
    }

    [Fact]
    public async Task MergePullRequest_OnFailure_ReturnsFalse()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("merge") == true, HttpStatusCode.Conflict);

        var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.MergePullRequest(client, "ws", "repo", 1, "msg", "merge_commit");

        Assert.False(result);
    }

    [Fact]
    public async Task CreateBranch_OnSuccess_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("refs/branches") == true, HttpStatusCode.Created);

        var client = new HttpClient(handler);
        var service = CreateService(client);
        var creds = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" };

        var result = await service.CreateBranch(client, creds, "ws", "repo", "feature", "abc123");

        Assert.True(result);
    }

    [Fact]
    public async Task UpdatePullRequest_OnSuccess_ReturnsTrue()
    {
        var handler = new MockHttpMessageHandler();
        handler.Setup(req => req.RequestUri?.ToString().Contains("pullrequests") == true && req.Method == HttpMethod.Put,
            HttpStatusCode.OK);

        var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.UpdatePullRequest(client, "ws", "repo", 1, "feature", "Title");

        Assert.True(result);
    }
}
