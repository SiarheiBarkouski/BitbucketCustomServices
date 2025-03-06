using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record DiffChanges(
    [property: JsonPropertyName("added")] bool? Added,
    [property: JsonPropertyName("removed")] bool? Removed
);