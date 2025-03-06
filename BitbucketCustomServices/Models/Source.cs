using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Source(
    [property: JsonPropertyName("repository")] Repository Repository,
    [property: JsonPropertyName("branch")] Branch Branch,
    [property: JsonPropertyName("commit")] Commit Commit);