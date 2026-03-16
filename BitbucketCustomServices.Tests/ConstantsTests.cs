using BitbucketCustomServices.Constants;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class ConstantsTests
{
    [Fact]
    public void PullRequestConstants_HasExpectedValues()
    {
        Assert.Equal("{0} for PR #{1} ({2})", PullRequestConstants.AutoMergeTitle);
        Assert.Equal("Auto branch merge conflict for PR #{0} ({1})", PullRequestConstants.AutoMergeConflictTitle);
        Assert.Equal("Auto branch merge", PullRequestConstants.AutoMergeTitlePartToDetectIgnore);
        Assert.Equal("Initial PR Id: {0}; Initial PR author: {1};", PullRequestConstants.AutoMergeDescription);
        Assert.Equal("Auto branch merge {0} → {1} for PR #{2} ({3})", PullRequestConstants.AutoMergeMessage);
    }

    [Fact]
    public void BranchConstants_HasExpectedValue()
    {
        Assert.Equal("cascade.merge/conflict-from-{0}-to-{1}-{2}", BranchConstants.ConflictBranchName);
    }
}
