using System.Text.Json.Serialization;
using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Requests.Bitbucket;

public record BranchCreateRequest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("target")] CommitReference Target);