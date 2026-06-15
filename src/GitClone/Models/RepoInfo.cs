namespace GitClone.Models;

/// <summary>One GitHub repo as shown in the backup list.</summary>
public sealed class RepoInfo
{
    public string Owner { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public bool Private { get; init; }
    public string DefaultBranch { get; init; } = "main";
    public string[] Topics { get; init; } = Array.Empty<string>();
    public string CloneUrl { get; init; } = "";
    public DateTimeOffset? PushedAt { get; init; }
    public long SizeKb { get; init; }

    public string FullName => $"{Owner}/{Name}";
}
