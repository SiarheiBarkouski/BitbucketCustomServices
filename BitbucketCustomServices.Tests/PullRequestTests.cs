using BitbucketCustomServices.Models;
using Xunit;

namespace BitbucketCustomServices.Tests;

public class PullRequestTests
{
    private static PullRequest CreatePullRequest(
        List<Reviewer>? reviewers = null,
        List<Participant>? participants = null,
        string description = "")
    {
        var destRepo = new Repository("workspace/repo");
        var destBranch = new Branch("main");
        var destCommit = new Commit("abc123", DateTime.UtcNow, "msg", new Author("a", "a", null!));
        var dest = new Destination(destRepo, destBranch, destCommit);

        var srcRepo = new Repository("workspace/repo");
        var srcBranch = new Branch("feature");
        var srcCommit = new Commit("def456", DateTime.UtcNow, "msg", new Author("a", "a", null!));
        var src = new Source(srcRepo, srcBranch, srcCommit);

        var author = new Author("Dev", "dev@example.com", new User("u1", "Dev"));

        return new PullRequest(
            dest, src, author, "Title", description, 1,
            reviewers ?? [],
            participants ?? []);
    }

    [Fact]
    public void IsFullyApproved_WhenNoReviewers_ReturnsFalse()
    {
        var pr = CreatePullRequest(reviewers: [], participants: []);
        Assert.False(pr.IsFullyApproved());
    }

    [Fact]
    public void IsFullyApproved_WhenNoParticipants_ReturnsFalse()
    {
        var pr = CreatePullRequest(
            reviewers: [new Reviewer("Alice")],
            participants: []);
        Assert.False(pr.IsFullyApproved());
    }

    [Fact]
    public void IsFullyApproved_WhenReviewerNotApproved_ReturnsFalse()
    {
        var pr = CreatePullRequest(
            reviewers: [new Reviewer("Alice")],
            participants: [new Participant(new User("u1", "Alice"), Approved: false, RequestedChanges: false)]);
        Assert.False(pr.IsFullyApproved());
    }

    [Fact]
    public void IsFullyApproved_WhenAllApproved_ReturnsTrue()
    {
        var pr = CreatePullRequest(
            reviewers: [new Reviewer("Alice"), new Reviewer("Bob")],
            participants: [
                new Participant(new User("u1", "Alice"), Approved: true, RequestedChanges: false),
                new Participant(new User("u2", "Bob"), Approved: true, RequestedChanges: false)
            ]);
        Assert.True(pr.IsFullyApproved());
    }

    [Fact]
    public void IsFullyApproved_WhenReviewerMissingFromParticipants_ReturnsFalse()
    {
        var pr = CreatePullRequest(
            reviewers: [new Reviewer("Alice")],
            participants: [new Participant(new User("u2", "Bob"), Approved: true, RequestedChanges: false)]);
        Assert.False(pr.IsFullyApproved());
    }

    [Fact]
    public void GetInitialPrAuthor_WhenDescriptionHasAuthor_ReturnsAuthor()
    {
        var pr = CreatePullRequest(description: "Initial PR Id: 42; Initial PR author: john.doe;");
        Assert.Equal("john.doe", pr.GetInitialPrAuthor());
    }

    [Fact]
    public void GetInitialPrAuthor_WhenDescriptionEmpty_ReturnsNull()
    {
        var pr = CreatePullRequest(description: "");
        Assert.Null(pr.GetInitialPrAuthor());
    }

    [Fact]
    public void GetInitialPrAuthor_WhenNoMatch_ReturnsNull()
    {
        var pr = CreatePullRequest(description: "Some other text");
        Assert.Null(pr.GetInitialPrAuthor());
    }

    [Fact]
    public void GetInitialPrId_WhenDescriptionHasId_ReturnsId()
    {
        var pr = CreatePullRequest(description: "Initial PR Id: 42; Initial PR author: john;");
        Assert.Equal(42, pr.GetInitialPrId());
    }

    [Fact]
    public void GetInitialPrId_WhenDescriptionEmpty_ReturnsNull()
    {
        var pr = CreatePullRequest(description: "");
        Assert.Null(pr.GetInitialPrId());
    }

    [Fact]
    public void GetInitialPrId_WhenNoMatch_ReturnsNull()
    {
        var pr = CreatePullRequest(description: "No id here");
        Assert.Null(pr.GetInitialPrId());
    }
}
