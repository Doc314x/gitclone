using System.IO.Compression;
using System.Text.Json;
using GitClone.Models;
using GitClone.Services;
using LibGit2Sharp;
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

    [Fact]
    public void Verify_FailsForEmptyMirror()
    {
        using var fx = new GitTestRepo();

        // Hand-build an archive whose mirror has zero refs and metadata that claims 0/0.
        string staging = fx.TempDir("empty-stage");
        Directory.CreateDirectory(staging);
        Repository.Init(Path.Combine(staging, "repo.git"), isBare: true);
        File.WriteAllText(
            Path.Combine(staging, "metadata.json"),
            JsonSerializer.Serialize(new ArchiveMetadata { Name = "empty", RefCount = 0, CommitCount = 0 }));

        string outDir = fx.TempDir("empty-out");
        Directory.CreateDirectory(outDir);
        string zip = Path.Combine(outDir, "empty.zip");
        ZipFile.CreateFromDirectory(staging, zip, CompressionLevel.Optimal, includeBaseDirectory: false);

        var result = new GitArchiver().Verify(zip);

        Assert.False(result.Ok);  // 0 refs must be rejected, not treated as "0 == 0 OK"
    }
}
