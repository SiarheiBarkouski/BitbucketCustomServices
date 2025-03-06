using System.Text.Json.Serialization;
using BitbucketCustomServices.Models;

namespace BitbucketCustomServices.Requests.Bitbucket;

public record PullRequestUpdateRequest(
    [property: JsonPropertyName("source")] BranchReference Source,
    [property: JsonPropertyName("title")] string Title);