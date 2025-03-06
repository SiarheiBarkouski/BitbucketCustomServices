namespace BitbucketCustomServices.Constants;

public static class PullRequestConstants
{
    public const string AutoMergeTitle = "{0} for PR #{1} ({2})";
    public const string AutoMergeConflictTitle = "Auto branch merge conflict for PR #{0} ({1})";
    public const string AutoMergeTitlePartToDetectIgnore = "Auto branch merge";

    public const string AutoMergeDescription = "Initial PR Id: {0}; Initial PR author: {1};";
    
    public const string AutoMergeMessage = "Auto branch merge {0} → {1} for PR #{2} ({3})";
}