using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class BitbucketServiceTests
{
    [Fact]
    public async Task GetAuthenticatedClient_WhenBasicPasswordAuth_SetsUsernamePasswordBasicHeader()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = new Mock<ILogger<BitbucketService>>();

        var service = new BitbucketService(factory, logger.Object);
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicPasswordAuth,
            Username = "user@example.com",
            Password = "mytoken"
        };

        var client = await service.GetAuthenticatedClient(creds);

        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Basic", client.DefaultRequestHeaders.Authorization.Scheme);
        var expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user@example.com:mytoken"));
        Assert.Equal(expected, client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAuthenticatedClient_WhenBasicTokenAuth_UsesEmailAndTokenForBasicHeader()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = new Mock<ILogger<BitbucketService>>();

        var service = new BitbucketService(factory, logger.Object);
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicTokenAuth,
            Email = "user@atlassian.com",
            Token = "ATATT3xAppPassword"
        };

        var client = await service.GetAuthenticatedClient(creds);

        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Basic", client.DefaultRequestHeaders.Authorization.Scheme);
        var expected = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("user@atlassian.com:ATATT3xAppPassword"));
        Assert.Equal(expected, client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAuthenticatedClient_WhenAuthToken_SetsBearerHeader()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = new Mock<ILogger<BitbucketService>>();

        var service = new BitbucketService(factory, logger.Object);
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.AuthToken,
            Token = "ATCTT3xSecretToken"
        };

        var client = await service.GetAuthenticatedClient(creds);

        Assert.NotNull(client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", client.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal("ATCTT3xSecretToken", client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAuthenticatedClient_WhenAuthToken_SetsAcceptJson()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = new Mock<ILogger<BitbucketService>>();

        var service = new BitbucketService(factory, logger.Object);
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.AuthToken,
            Token = "token"
        };

        var client = await service.GetAuthenticatedClient(creds);

        Assert.Contains(client.DefaultRequestHeaders.Accept, h =>
            h.MediaType == "application/json");
    }

    [Fact]
    public async Task GetAuthenticatedClient_WhenInvalidAuthType_Throws()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = new Mock<ILogger<BitbucketService>>();

        var service = new BitbucketService(factory, logger.Object);
        var creds = new RepositoryCredentials
        {
            AuthType = (AuthType)99,
            Token = "token"
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetAuthenticatedClient(creds));
    }
}
