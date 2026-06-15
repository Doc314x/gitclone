using GitClone.Services;
using LibGit2Sharp;
using Xunit;

namespace GitClone.Tests;

public class GitStatsTests
{
    [Fact]
    public void Counts_BranchesTagsAndCommits()
    {
        using var fx = new GitTestRepo();
        string bare = fx.CreateSourceRepo();

        using var repo = new Repository(bare);
        var stats = GitStats.Compute(repo);

        // 2 branches (main, feature) + 1 tag => 3 refs.
        Assert.Equal(3, stats.RefCount);
        // 3 commits, both branches share the same history tip here.
        Assert.Equal(3, stats.CommitCount);
    }
}
