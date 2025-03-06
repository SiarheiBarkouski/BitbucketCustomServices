using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record User(
    [property: JsonPropertyName("uuid")] string Id,
    [property: JsonPropertyName("display_name")] string DisplayName);