using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class RepositoryCredentialsTests
{
    [Fact]
    public void Validate_BasicAuth_WhenUsernameEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.Basic,
            Username = "",
            Password = "token123"
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Username", message!);
    }

    [Fact]
    public void Validate_BasicAuth_WhenPasswordEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.Basic,
            Username = "user@example.com",
            Password = ""
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Password", message!);
    }

    [Fact]
    public void Validate_BasicAuth_WhenBothSet_ReturnsTrue()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.Basic,
            Username = "user@example.com",
            Password = "token123"
        };
        var (valid, _) = creds.Validate();
        Assert.True(valid);
    }

    [Fact]
    public void Validate_AuthToken_WhenTokenEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.AuthToken,
            Token = ""
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Token", message!);
    }

    [Fact]
    public void Validate_AuthToken_WhenTokenSet_ReturnsTrue()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.AuthToken,
            Token = "ATCTT3x..."
        };
        var (valid, _) = creds.Validate();
        Assert.True(valid);
    }
}
