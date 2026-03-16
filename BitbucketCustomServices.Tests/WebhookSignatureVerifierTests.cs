using BitbucketCustomServices.Services;
using System.Text;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class WebhookSignatureVerifierTests
{
    [Fact]
    public void Verify_WhenSecretIsNull_ReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.True(WebhookSignatureVerifier.Verify(null, body, "sha256=abc"));
    }

    [Fact]
    public void Verify_WhenSecretIsEmpty_ReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.True(WebhookSignatureVerifier.Verify("", body, "sha256=abc"));
    }

    [Fact]
    public void Verify_WhenSecretIsWhitespace_ReturnsTrue()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.True(WebhookSignatureVerifier.Verify("   ", body, "sha256=abc"));
    }

    [Fact]
    public void Verify_WhenSignatureHeaderIsNull_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.False(WebhookSignatureVerifier.Verify("secret", body, null));
    }

    [Fact]
    public void Verify_WhenSignatureHeaderIsEmpty_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.False(WebhookSignatureVerifier.Verify("secret", body, ""));
    }

    [Fact]
    public void Verify_WhenSignatureHeaderMissingPrefix_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.False(WebhookSignatureVerifier.Verify("secret", body, "invalid"));
    }

    [Fact]
    public void Verify_WhenSignatureHeaderHasWrongPrefix_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.False(WebhookSignatureVerifier.Verify("secret", body, "md5=abc123"));
    }

    [Fact]
    public void Verify_WhenSignatureHexIsInvalid_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.False(WebhookSignatureVerifier.Verify("secret", body, "sha256=nothex"));
    }

    [Fact]
    public void Verify_WhenSignatureHexIsOddLength_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{}");
        Assert.False(WebhookSignatureVerifier.Verify("secret", body, "sha256=abc"));
    }

    [Fact]
    public void Verify_WhenSignatureMismatch_ReturnsFalse()
    {
        var body = Encoding.UTF8.GetBytes("{\"test\":1}");
        var wrongSignature = Convert.ToHexString(Encoding.UTF8.GetBytes("wrong")).ToLowerInvariant();
        Assert.False(WebhookSignatureVerifier.Verify("mysecret", body, "sha256=" + wrongSignature));
    }

    [Fact]
    public void Verify_WhenSignatureMatches_ReturnsTrue()
    {
        var secret = "my-webhook-secret";
        var body = Encoding.UTF8.GetBytes("{\"event\":\"pullrequest:fulfilled\"}");
        var hash = System.Security.Cryptography.HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        var signature = "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();

        Assert.True(WebhookSignatureVerifier.Verify(secret, body, signature));
    }

    [Fact]
    public void Verify_WhenSignatureMatchesUppercaseHex_ReturnsTrue()
    {
        var secret = "my-webhook-secret";
        var body = Encoding.UTF8.GetBytes("{\"event\":\"pullrequest:fulfilled\"}");
        var hash = System.Security.Cryptography.HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), body);
        var signature = "sha256=" + Convert.ToHexString(hash);

        Assert.True(WebhookSignatureVerifier.Verify(secret, body, signature));
    }
}
