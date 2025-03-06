using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record PullRequestsResponse(
    [property: JsonPropertyName("values")] List<PullRequest> Values,
    [property: JsonPropertyName("pagelen")] int PageLength,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("size")] int Size
); 