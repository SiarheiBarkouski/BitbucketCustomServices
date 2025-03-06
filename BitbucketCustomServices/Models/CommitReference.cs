using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record CommitReference([property: JsonPropertyName("hash")] string Hash);