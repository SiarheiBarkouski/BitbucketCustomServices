using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Reviewer(
    [property: JsonPropertyName("display_name")] string DisplayName);