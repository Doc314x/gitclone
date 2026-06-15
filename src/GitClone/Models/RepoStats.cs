namespace GitClone.Models;

/// <summary>Cheap fingerprint of a bare repo, compared before/after zipping to verify integrity.</summary>
public readonly record struct RepoStats(int RefCount, int CommitCount);
