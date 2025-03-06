using System.Runtime.Serialization;

namespace BitbucketCustomServices.Enums;

[Flags]
public enum EventType : long
{
    Default = 0,

    [EnumMember(Value = "pullrequest:created")]
    PullRequestCreated = 1 << 0,

    [EnumMember(Value = "pullrequest:changes_request_created")]
    PullRequestChangesRequested = 1 << 1,

    [EnumMember(Value = "pullrequest:rejected")]
    PullRequestDeclined = 1 << 2,

    [EnumMember(Value = "pullrequest:fulfilled")]
    PullRequestMerged = 1 << 3,

    [EnumMember(Value = "pullrequest:approved")]
    PullRequestApproved = 1 << 4,

    [EnumMember(Value = "pullrequest:updated")]
    PullRequestUpdated = 1 << 5,
    
    MergeConflict = 1 << 6,
}