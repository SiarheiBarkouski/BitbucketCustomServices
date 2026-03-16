using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class RepositoryServiceTests
{
    private static RepositoryService CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        var logger = new Mock<ILogger<RepositoryService>>();
        return new RepositoryService(dbContext, logger.Object);
    }

    [Fact]
    public void ValidateRepositoryCredentials_WhenCredentialsNull_ReturnsFalse()
    {
        var service = CreateService();
        var repo = new Repository { Name = "test", RepositoryCredentials = null! };
        Assert.False(service.ValidateRepositoryCredentials(repo));
    }

    [Fact]
    public void ValidateRepositoryCredentials_WhenBasicAuthValid_ReturnsTrue()
    {
        var service = CreateService();
        var repo = new Repository
        {
            Name = "test",
            RepositoryCredentials = new RepositoryCredentials
            {
                AuthType = AuthType.Basic,
                Username = "user@example.com",
                Password = "token"
            }
        };
        Assert.True(service.ValidateRepositoryCredentials(repo));
    }

    [Fact]
    public void ValidateRepositoryCredentials_WhenBasicAuthInvalid_ReturnsFalse()
    {
        var service = CreateService();
        var repo = new Repository
        {
            Name = "test",
            RepositoryCredentials = new RepositoryCredentials
            {
                AuthType = AuthType.Basic,
                Username = "",
                Password = "token"
            }
        };
        Assert.False(service.ValidateRepositoryCredentials(repo));
    }

    [Fact]
    public void ValidateRepositoryCredentials_WhenAuthTokenValid_ReturnsTrue()
    {
        var service = CreateService();
        var repo = new Repository
        {
            Name = "test",
            RepositoryCredentials = new RepositoryCredentials
            {
                AuthType = AuthType.AuthToken,
                Token = "ATCTT3x..."
            }
        };
        Assert.True(service.ValidateRepositoryCredentials(repo));
    }

    [Fact]
    public void ValidateRepositoryCredentials_WhenAuthTokenInvalid_ReturnsFalse()
    {
        var service = CreateService();
        var repo = new Repository
        {
            Name = "test",
            RepositoryCredentials = new RepositoryCredentials
            {
                AuthType = AuthType.AuthToken,
                Token = ""
            }
        };
        Assert.False(service.ValidateRepositoryCredentials(repo));
    }

    [Fact]
    public async Task GetRepositoryByWorkspaceAndSlug_WhenFound_ReturnsRepository()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new AppDbContext(options);
        var project = new Project { Name = "ws" };
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();
        var repo = new Repository
        {
            Name = "repo",
            ProjectId = project.Id,
            MergeStrategy = "merge_commit",
            RepositoryCredentials = new RepositoryCredentials { AuthType = AuthType.AuthToken, Token = "t" },
            RepositoryNotificationSettings = new RepositoryNotificationSettings { EventType = EventType.Default },
            BranchMappings = []
        };
        dbContext.Repositories.Add(repo);
        await dbContext.SaveChangesAsync();

        var logger = new Mock<ILogger<RepositoryService>>();
        var service = new RepositoryService(dbContext, logger.Object);

        var result = await service.GetRepositoryByWorkspaceAndSlug("ws", "repo");

        Assert.NotNull(result);
        Assert.Equal("repo", result.Name);
    }

    [Fact]
    public async Task GetRepositoryByWorkspaceAndSlug_WhenNotFound_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.GetRepositoryByWorkspaceAndSlug("nonexistent", "repo");
        Assert.Null(result);
    }
}
