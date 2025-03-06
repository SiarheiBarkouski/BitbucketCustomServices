using System.Text.Json.Serialization;
using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Requests.Bitbucket;

public record PullRequestCreateRequest(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("source")] BranchReference Source,
    [property: JsonPropertyName("destination")] BranchReference Destination,
    [property: JsonPropertyName("description")] string Description = "");