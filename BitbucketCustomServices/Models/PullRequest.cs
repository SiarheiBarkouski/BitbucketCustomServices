using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BitbucketCustomServices.Models;

public record PullRequest(
    [property: JsonPropertyName("destination")] Destination Destination,
    [property: JsonPropertyName("source")] Source Source,
    [property: JsonPropertyName("author")] Author Author,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("reviewers")] List<Reviewer> Reviewers,
    [property: JsonPropertyName("participants")] List<Participant> Participants)
{
    public bool IsFullyApproved()
    {
        if (Reviewers == null || Reviewers.Count == 0)
            return false;

        if (Participants == null || Participants.Count == 0)
            return false;

        foreach (var reviewer in Reviewers)
        {
            var participant = Participants.FirstOrDefault(x => x.User?.DisplayName == reviewer.DisplayName);

            if (participant is null || !participant.Approved)
                return false;
        }

        return true;
    }

    public string GetInitialPrAuthor()
    {
        var value = Regex.Match(Description, "(?<=Initial PR author: ).*(?=;)");

        if (!value.Success || string.IsNullOrWhiteSpace(value.ToString().Trim()))
            return null;
        
        return value.ToString();
    }

    public int? GetInitialPrId()
    {
        var value = Regex.Match(Description, "(?<=Initial PR Id: )[0-9]*(?=;)");
        var isParsed = int.TryParse(value.ToString(), out var result);

        return isParsed ? result : null;
    }
}