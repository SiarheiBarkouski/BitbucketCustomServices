using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record Participant(
    [property: JsonPropertyName("user")] User User,
    [property: JsonPropertyName("approved")] bool Approved,
    [property: JsonPropertyName("request_changes")] bool RequestedChanges);