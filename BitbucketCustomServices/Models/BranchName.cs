using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record BranchName([property: JsonPropertyName("name")] string Name);