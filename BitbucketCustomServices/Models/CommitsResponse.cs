using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record CommitsResponse(
    [property: JsonPropertyName("values")] List<Commit> Values,
    [property: JsonPropertyName("pagelen")] int PageLength,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("size")] int Size
);