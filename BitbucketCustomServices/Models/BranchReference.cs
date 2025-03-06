using System.Text.Json.Serialization;

namespace BitbucketCustomServices.Models;

public record BranchReference(
    [property: JsonPropertyName("branch")] BranchName Branch)
{
    public BranchReference(string name) : this(new BranchName(name))
    {
    }
}