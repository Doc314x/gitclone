using System.IO.Compression;
using System.Text.Json;
using GitClone.Models;
using GitClone.Services;
using Xunit;

namespace GitClone.Tests;

public class GitArchiverTests
{
    [Fact]
    public void Archive_ProducesZipWithMirrorAndMetadata()
    {
        using var fx = new GitTestRepo();
        string source = fx.CreateSourceRepo();

        var info = new RepoInfo
        {
            Owner = "Doc314x",
            Name = "demo",
            Description = "demo",
            Private = true,
            DefaultBranch = "master",
            CloneUrl = source,             // local path stands in for the GitHub clone url
            Topics = new[] { "x" }
        };

        var archiver = new GitArchiver();
        string zipPath = archiver.Archive(info, fx.TempDir("out"), token: "unused-for-local-path");

        Assert.True(File.Exists(zipPath));
        Assert.Equal("Doc314x__demo.zip", Path.GetFileName(zipPath));

        using var zip = ZipFile.OpenRead(zipPath);
        Assert.Contains(zip.Entries, e => e.FullName == "metadata.json");
        Assert.Contains(zip.Entries, e => e.FullName.StartsWith("repo.git/"));

        var metaEntry = zip.GetEntry("metadata.json")!;
        using var reader = new StreamReader(metaEntry.Open());
        var meta = JsonSerializer.Deserialize<ArchiveMetadata>(reader.ReadToEnd())!;
        Assert.Equal("demo", meta.Name);
        Assert.Equal(3, meta.RefCount);   // main, feature, v1.0
        Assert.Equal(3, meta.CommitCount);
    }
}
