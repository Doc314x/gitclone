using GitClone.Models;
using GitClone.Services;
using LibGit2Sharp;
using Xunit;

namespace GitClone.Tests;

public class GitRestorerTests
{
    [Fact]
    public void Restore_PushesAllRefsToTargetRemote()
    {
        using var fx = new GitTestRepo();

        // Build an archive from a source repo.
        string source = fx.CreateSourceRepo();
        string zip = new GitArchiver().Archive(
            new RepoInfo { Owner = "Doc314x", Name = "demo", CloneUrl = source }, fx.TempDir("out"), token: "");

        // Empty bare repo stands in for the freshly-created GitHub repo.
        string target = fx.CreateEmptyBareRepo();

        new GitRestorer().PushArchiveTo(zip, target, token: "");

        using var repo = new Repository(target);
        var stats = GitStats.Compute(repo);
        Assert.Equal(3, stats.RefCount);     // main, feature, v1.0
        Assert.Equal(3, stats.CommitCount);
    }
}
