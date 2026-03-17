using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class SeedConfigHelperCreateRepositoryTests
{
    [Fact]
    public void CreateRepositoryFromConfig_WithBasicTokenAuth_CreatesRepositoryWithBasicTokenCredentials()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            MergeStrategy = "merge_commit",
            UserEmail = "user@example.com",
            UserToken = "token123",
            BranchMappings = [new SeedBranchMappingConfiguration { From = "main", To = "develop" }]
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.Equal("my-repo", repo.Name);
        Assert.Equal("merge_commit", repo.MergeStrategy);
        Assert.Equal(AuthType.BasicTokenAuth, repo.RepositoryCredentials.AuthType);
        Assert.Equal("user@example.com", repo.RepositoryCredentials.Email);
        Assert.Equal("token123", repo.RepositoryCredentials.Token);
        Assert.Single(repo.BranchMappings);
        Assert.Equal("main", repo.BranchMappings[0].From);
        Assert.Equal("develop", repo.BranchMappings[0].To);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithBasicPasswordAuth_CreatesRepositoryWithBasicPasswordCredentials()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            AuthType = "BasicPasswordAuth",
            UserName = "myuser",
            Password = "mypassword",
            BranchMappings = []
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.Equal(AuthType.BasicPasswordAuth, repo.RepositoryCredentials.AuthType);
        Assert.Equal("myuser", repo.RepositoryCredentials.Username);
        Assert.Equal("mypassword", repo.RepositoryCredentials.Password);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithAuthToken_CreatesRepositoryWithTokenCredentials()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            BitbucketToken = "ATCTT3x...",
            BranchMappings = []
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.Equal(AuthType.AuthToken, repo.RepositoryCredentials.AuthType);
        Assert.Equal("ATCTT3x...", repo.RepositoryCredentials.Token);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithWebhookSecret_SetsWebhookSecret()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            WebhookSecret = " my-secret "
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.Equal("my-secret", repo.WebhookSecret);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithEmptyWebhookSecret_SetsNull()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            WebhookSecret = ""
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.Null(repo.WebhookSecret);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithNotificationSettings_SetsIgnoreAutoMerge()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            NotificationSettings = new SeedRepositoryNotificationSettings { IgnoreAutoMergeNotifications = true }
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.True(repo.RepositoryNotificationSettings.IgnoreAutoMergeNotifications);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithFeatureSwitches_SetsFlags()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            CascadeMergeEnabled = false,
            TelegramNotificationsEnabled = false,
            BranchMappings = []
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.False(repo.CascadeMergeEnabled);
        Assert.False(repo.TelegramNotificationsEnabled);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithoutFeatureSwitches_DefaultsToFalse()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            BranchMappings = []
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.False(repo.CascadeMergeEnabled);
        Assert.False(repo.TelegramNotificationsEnabled);
    }

    [Fact]
    public void CreateRepositoryFromConfig_WithTelegram_SetsTelegramFields()
    {
        var config = new SeedRepositoryConfiguration
        {
            Name = "my-repo",
            TelegramBotToken = "bot123",
            TelegramChatId = "chat456"
        };

        var repo = SeedConfigHelper.CreateRepositoryFromConfig(config);

        Assert.Equal("bot123", repo.TelegramBotToken);
        Assert.Equal("chat456", repo.TelegramChatId);
    }
}
