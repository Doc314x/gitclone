namespace GitClone.Models;

/// <summary>Persisted as metadata.json inside each archive zip. Drives the restore.</summary>
public sealed class ArchiveMetadata
{
    public string Owner { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool Private { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public string[] Topics { get; set; } = Array.Empty<string>();
    public string SourceUrl { get; set; } = "";
    public DateTimeOffset ArchivedAt { get; set; }
    public int RefCount { get; set; }
    public int CommitCount { get; set; }
}
