using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record CommitChanges(
    [property: JsonPropertyName("added")] IReadOnlyList<Commit> Added
);