using System.Security.Cryptography;
using System.Text;

namespace BitbucketCustomServices.Services;

public static class WebhookSignatureVerifier
{
    private const string SignaturePrefix = "sha256=";

    /// <summary>
    /// Verifies Bitbucket Cloud webhook signature (X-Hub-Signature header).
    /// Returns true if secret is null/empty (optional - allow through) or if signature is valid.
    /// Returns false if secret is set but signature is invalid or missing.
    /// </summary>
    public static bool Verify(string? secret, byte[] rawBody, string? signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return true;

        if (string.IsNullOrWhiteSpace(signatureHeader) || !signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedHex = signatureHeader[SignaturePrefix.Length..].Trim();
        if (string.IsNullOrEmpty(expectedHex) || expectedHex.Length % 2 != 0)
            return false;

        try
        {
            var expectedBytes = Convert.FromHexString(expectedHex);
            var computedHash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), rawBody);

            return CryptographicOperations.FixedTimeEquals(computedHash, expectedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
