using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Destination(
    [property: JsonPropertyName("repository")] Repository Repository,
    [property: JsonPropertyName("branch")] Branch Branch,
    [property: JsonPropertyName("commit")] Commit Commit);