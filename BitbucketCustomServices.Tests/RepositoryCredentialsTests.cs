using BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class RepositoryCredentialsTests
{
    [Fact]
    public void Validate_BasicPasswordAuth_WhenUsernameEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicPasswordAuth,
            Username = "",
            Password = "token123"
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Username", message!);
    }

    [Fact]
    public void Validate_BasicPasswordAuth_WhenPasswordEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicPasswordAuth,
            Username = "user@example.com",
            Password = ""
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Password", message!);
    }

    [Fact]
    public void Validate_BasicPasswordAuth_WhenBothSet_ReturnsTrue()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicPasswordAuth,
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

    [Fact]
    public void Validate_BasicTokenAuth_WhenEmailEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicTokenAuth,
            Email = "",
            Token = "token123"
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Email", message!);
    }

    [Fact]
    public void Validate_BasicTokenAuth_WhenTokenEmpty_ReturnsFalse()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicTokenAuth,
            Email = "user@example.com",
            Token = ""
        };
        var (valid, message) = creds.Validate();
        Assert.False(valid);
        Assert.Contains("Token", message!);
    }

    [Fact]
    public void Validate_BasicTokenAuth_WhenBothSet_ReturnsTrue()
    {
        var creds = new RepositoryCredentials
        {
            AuthType = AuthType.BasicTokenAuth,
            Email = "user@example.com",
            Token = "token123"
        };
        var (valid, _) = creds.Validate();
        Assert.True(valid);
    }
}
