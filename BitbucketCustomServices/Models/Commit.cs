using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Commit(
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("author")] Author Author
);