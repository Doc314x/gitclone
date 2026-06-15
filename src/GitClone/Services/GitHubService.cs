using GitClone.Models;
using Octokit;

namespace GitClone.Services;

public sealed class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;

    public GitHubService(string token)
    {
        _client = new GitHubClient(new ProductHeaderValue(AppConfig.ProductName))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<IReadOnlyList<RepoInfo>> ListReposAsync()
    {
        var request = new RepositoryRequest
        {
            Affiliation = RepositoryAffiliation.Owner,
            Sort = RepositorySort.Pushed,
            Direction = SortDirection.Ascending  // least-recently pushed first => "unused" on top
        };
        var repos = await _client.Repository.GetAllForCurrent(request);

        return repos.Select(r => new RepoInfo
        {
            Owner = r.Owner.Login,
            Name = r.Name,
            Description = r.Description,
            Private = r.Private,
            DefaultBranch = r.DefaultBranch ?? "main",
            Topics = Array.Empty<string>(),  // Octokit's Repository model doesn't expose topics directly
            CloneUrl = r.CloneUrl,
            PushedAt = r.PushedAt,
            SizeKb = r.Size
        }).ToList();
    }

    public async Task<RepoInfo> CreateRepoAsync(ArchiveMetadata meta)
    {
        var newRepo = new NewRepository(meta.Name)
        {
            Description = meta.Description,
            Private = meta.Private,
            AutoInit = false
        };
        var created = await _client.Repository.Create(newRepo);

        return new RepoInfo
        {
            Owner = created.Owner.Login,
            Name = created.Name,
            Private = created.Private,
            DefaultBranch = created.DefaultBranch ?? meta.DefaultBranch,
            CloneUrl = created.CloneUrl
        };
    }

    public Task DeleteRepoAsync(string owner, string name) =>
        _client.Repository.Delete(owner, name);
}
