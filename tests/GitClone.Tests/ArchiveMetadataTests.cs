using System.Text.Json;
using GitClone.Models;
using Xunit;

namespace GitClone.Tests;

public class ArchiveMetadataTests
{
    [Fact]
    public void RoundTrips_ThroughJson()
    {
        var meta = new ArchiveMetadata
        {
            Owner = "Doc314x",
            Name = "demo",
            Description = "a demo repo",
            Private = true,
            DefaultBranch = "main",
            Topics = new[] { "tooling", "backup" },
            SourceUrl = "https://github.com/Doc314x/demo.git",
            ArchivedAt = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero),
            RefCount = 7,
            CommitCount = 142
        };

        string json = JsonSerializer.Serialize(meta);
        var back = JsonSerializer.Deserialize<ArchiveMetadata>(json)!;

        Assert.Equal("Doc314x", back.Owner);
        Assert.Equal("demo", back.Name);
        Assert.True(back.Private);
        Assert.Equal("main", back.DefaultBranch);
        Assert.Equal(new[] { "tooling", "backup" }, back.Topics);
        Assert.Equal(7, back.RefCount);
        Assert.Equal(142, back.CommitCount);
    }
}
