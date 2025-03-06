using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Changes(
    [property: JsonPropertyName("commits")] CommitChanges Commits,
    [property: JsonPropertyName("diff")] DiffChanges Diff
);