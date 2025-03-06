using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Actor(
    [property: JsonPropertyName("display_name")] string DisplayName);