using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Requests.Bitbucket;

public record PullRequestMergeRequest(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("merge_strategy")] string MergeStrategy);