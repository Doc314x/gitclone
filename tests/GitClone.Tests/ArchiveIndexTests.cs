using GitClone.Models;
using GitClone.Services;
using Xunit;

namespace GitClone.Tests;

public class ArchiveIndexTests
{
    [Fact]
    public void Lists_ArchivesFromMetadata_IgnoringOtherFiles()
    {
        using var fx = new GitTestRepo();
        string source = fx.CreateSourceRepo();
        string outDir = fx.TempDir("out");

        new GitArchiver().Archive(
            new RepoInfo { Owner = "Doc314x", Name = "demo", CloneUrl = source }, outDir, token: "");

        // A stray non-archive file must be ignored.
        File.WriteAllText(Path.Combine(outDir, "notes.txt"), "hello");

        var entries = new ArchiveIndex().List(outDir);

        var entry = Assert.Single(entries);
        Assert.Equal("demo", entry.Metadata.Name);
        Assert.Equal("Doc314x__demo.zip", Path.GetFileName(entry.ZipPath));
    }

    [Fact]
    public void Returns_Empty_ForMissingFolder()
    {
        var entries = new ArchiveIndex().List(Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid()));
        Assert.Empty(entries);
    }
}
