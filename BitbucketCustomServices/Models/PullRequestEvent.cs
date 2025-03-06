using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record PullRequestEvent(
    [property: JsonPropertyName("actor")] Actor Actor,
    [property: JsonPropertyName("pullrequest")] PullRequest PullRequest,
    [property: JsonPropertyName("changes")] Changes Changes
);