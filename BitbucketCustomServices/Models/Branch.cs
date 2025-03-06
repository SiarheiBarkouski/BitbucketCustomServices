using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Branch([property: JsonPropertyName("name")] string Name);