using GitClone.Models;

namespace GitClone.Services;

public interface IGitHubService
{
    Task<IReadOnlyList<RepoInfo>> ListReposAsync();
    Task<RepoInfo> CreateRepoAsync(ArchiveMetadata meta);
    Task DeleteRepoAsync(string owner, string name);
}
