using LibGit2Sharp;

namespace GitClone.Tests;

/// <summary>
/// Builds throwaway repos in a temp folder so git services can be tested without a network.
/// A "source" bare repo stands in for a GitHub remote.
/// </summary>
public sealed class GitTestRepo : IDisposable
{
    public string Root { get; }
    private readonly Signature _sig = new("Test", "test@example.com", DateTimeOffset.UtcNow);

    public GitTestRepo()
    {
        Root = Path.Combine(Path.GetTempPath(), "gitclone-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    /// <summary>Create a bare source repo seeded with 3 commits plus one extra branch and a tag.</summary>
    public string CreateSourceRepo(string name = "source")
    {
        string workdir = Path.Combine(Root, name + "-work");
        Repository.Init(workdir);
        using (var repo = new Repository(workdir))
        {
            for (int i = 0; i < 3; i++)
            {
                string file = Path.Combine(workdir, $"file{i}.txt");
                File.WriteAllText(file, $"content {i}");
                Commands.Stage(repo, file);
                repo.Commit($"commit {i}", _sig, _sig);
            }
            repo.CreateBranch("feature");
            repo.ApplyTag("v1.0");
        }

        // Turn the working repo into a bare repo by cloning it bare locally.
        string barePath = Path.Combine(Root, name + ".git");
        Repository.Clone(workdir, barePath, new CloneOptions { IsBare = true });
        return barePath;
    }

    /// <summary>An empty bare repo that stands in for a freshly-created GitHub repo (restore target).</summary>
    public string CreateEmptyBareRepo(string name = "target")
    {
        string barePath = Path.Combine(Root, name + ".git");
        Repository.Init(barePath, isBare: true);
        return barePath;
    }

    public string TempDir(string name) => Path.Combine(Root, name);

    public void Dispose()
    {
        try
        {
            // Git marks objects read-only; clear the attribute before delete.
            foreach (var f in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(Root, recursive: true);
        }
        catch { /* best-effort temp cleanup */ }
    }
}
