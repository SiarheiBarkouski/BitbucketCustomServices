using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Author(
    [property: JsonPropertyName("display_name")] string DisplayName,
    [property: JsonPropertyName("raw")] string Raw,
    [property: JsonPropertyName("user")] User User
);