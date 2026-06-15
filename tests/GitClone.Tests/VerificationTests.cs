using System.IO.Compression;
using GitClone.Models;
using GitClone.Services;
using Xunit;

namespace GitClone.Tests;

public class VerificationTests
{
    private static string MakeArchive(GitTestRepo fx)
    {
        string source = fx.CreateSourceRepo();
        var info = new RepoInfo { Owner = "Doc314x", Name = "demo", CloneUrl = source };
        return new GitArchiver().Archive(info, fx.TempDir("out"), token: "");
    }

    [Fact]
    public void Verify_PassesForGoodArchive()
    {
        using var fx = new GitTestRepo();
        string zip = MakeArchive(fx);

        var result = new GitArchiver().Verify(zip);

        Assert.True(result.Ok, result.Message);
    }

    [Fact]
    public void Verify_FailsWhenMetadataMissing()
    {
        using var fx = new GitTestRepo();
        string zip = MakeArchive(fx);

        // Corrupt the archive: drop metadata.json.
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
            archive.GetEntry("metadata.json")!.Delete();

        var result = new GitArchiver().Verify(zip);

        Assert.False(result.Ok);
    }
}
