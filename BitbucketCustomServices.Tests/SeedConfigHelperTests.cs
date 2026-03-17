using BitbucketCustomServices;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class SeedConfigHelperTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("Moderator")]
    [InlineData("User")]
    public void IsValidRole_WhenStandardRole_ReturnsTrue(string role)
    {
        Assert.True(SeedConfigHelper.IsValidRole(role));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("MODERATOR")]
    [InlineData("CustomRole")]
    [InlineData("Guest")]
    public void IsValidRole_WhenInvalid_ReturnsFalse(string? role)
    {
        Assert.False(SeedConfigHelper.IsValidRole(role));
    }

    [Fact]
    public void ResolveAuthType_WhenUserEmailAndUserTokenSet_ReturnsBasicTokenAuth()
    {
        var config = new SeedRepositoryConfiguration
        {
            UserEmail = "user@example.com",
            UserToken = "token123"
        };
        Assert.Equal(AuthType.BasicTokenAuth, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void ResolveAuthType_WhenUserNameAndPasswordSet_ReturnsBasicPasswordAuth()
    {
        var config = new SeedRepositoryConfiguration
        {
            UserName = "myuser",
            Password = "mypassword"
        };
        Assert.Equal(AuthType.BasicPasswordAuth, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void ResolveAuthType_WhenOnlyBitbucketTokenSet_ReturnsAuthToken()
    {
        var config = new SeedRepositoryConfiguration
        {
            BitbucketToken = "ATCTT3x..."
        };
        Assert.Equal(AuthType.AuthToken, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void ResolveAuthType_WhenAuthTypeExplicitlyBasicTokenAuthAndCredsSet_ReturnsBasicTokenAuth()
    {
        var config = new SeedRepositoryConfiguration
        {
            AuthType = "BasicTokenAuth",
            UserEmail = "user@example.com",
            UserToken = "token123"
        };
        Assert.Equal(AuthType.BasicTokenAuth, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void ResolveAuthType_WhenAuthTypeExplicitlyAuthToken_ReturnsAuthToken()
    {
        var config = new SeedRepositoryConfiguration
        {
            AuthType = "AuthToken",
            BitbucketToken = "ATCTT3x..."
        };
        Assert.Equal(AuthType.AuthToken, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void ResolveAuthType_WhenAuthTypeBasicTokenAuthButNoCreds_ReturnsAuthToken()
    {
        var config = new SeedRepositoryConfiguration
        {
            AuthType = "BasicTokenAuth",
            BitbucketToken = "ATCTT3x..."
        };
        Assert.Equal(AuthType.AuthToken, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void ResolveAuthType_WhenAuthTypeAuthTokenButHasBasicCreds_ReturnsAuthToken()
    {
        var config = new SeedRepositoryConfiguration
        {
            AuthType = "AuthToken",
            UserEmail = "user@example.com",
            UserToken = "token123"
        };
        Assert.Equal(AuthType.AuthToken, SeedConfigHelper.ResolveAuthType(config));
    }

    [Fact]
    public void IsValidUserConfig_WhenAllValid_ReturnsTrue()
    {
        var config = new SeedUserWithRoleConfiguration
        {
            UserName = "dev",
            Email = "dev@example.com",
            Password = "Pass123!",
            Role = "User"
        };
        Assert.True(SeedConfigHelper.IsValidUserConfig(config));
    }

    [Fact]
    public void IsValidUserConfig_WhenUserNameEmpty_ReturnsFalse()
    {
        var config = new SeedUserWithRoleConfiguration
        {
            UserName = "",
            Email = "dev@example.com",
            Password = "Pass123!",
            Role = "User"
        };
        Assert.False(SeedConfigHelper.IsValidUserConfig(config));
    }

    [Fact]
    public void IsValidUserConfig_WhenEmailEmpty_ReturnsFalse()
    {
        var config = new SeedUserWithRoleConfiguration
        {
            UserName = "dev",
            Email = "",
            Password = "Pass123!",
            Role = "User"
        };
        Assert.False(SeedConfigHelper.IsValidUserConfig(config));
    }

    [Fact]
    public void IsValidUserConfig_WhenPasswordEmpty_ReturnsFalse()
    {
        var config = new SeedUserWithRoleConfiguration
        {
            UserName = "dev",
            Email = "dev@example.com",
            Password = "",
            Role = "User"
        };
        Assert.False(SeedConfigHelper.IsValidUserConfig(config));
    }

    [Fact]
    public void IsValidUserConfig_WhenInvalidRole_ReturnsFalse()
    {
        var config = new SeedUserWithRoleConfiguration
        {
            UserName = "dev",
            Email = "dev@example.com",
            Password = "Pass123!",
            Role = "CustomRole"
        };
        Assert.False(SeedConfigHelper.IsValidUserConfig(config));
    }
}
