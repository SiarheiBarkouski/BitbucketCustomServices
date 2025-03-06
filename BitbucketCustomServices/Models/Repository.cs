using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Repository(
    [property: JsonPropertyName("full_name")] string FullName);