using System.Security.Cryptography;
using System.Text;

namespace BitbucketCustomServices.Helpers;

public static class HashHelper
{
    public static string ComputeSha256Hash(string rawData)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        
        foreach (var b in bytes) 
            builder.Append(b.ToString("x2"));
        
        return builder.ToString();
    }
}