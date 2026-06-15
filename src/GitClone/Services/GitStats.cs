using GitClone.Models;
using LibGit2Sharp;

namespace GitClone.Services;

/// <summary>Computes a cheap integrity fingerprint of a bare repo.</summary>
public static class GitStats
{
    public static RepoStats Compute(Repository repo)
    {
        int refCount = repo.Refs.Count(r =>
            r.CanonicalName.StartsWith("refs/heads/") ||
            r.CanonicalName.StartsWith("refs/tags/"));

        var filter = new CommitFilter { IncludeReachableFrom = repo.Refs.ToList() };
        int commitCount = repo.Commits.QueryBy(filter).Count();

        return new RepoStats(refCount, commitCount);
    }
}
