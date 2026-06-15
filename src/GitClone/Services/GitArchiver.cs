using System.IO.Compression;
using System.Text.Json;
using GitClone.Models;
using LibGit2Sharp;

namespace GitClone.Services;

/// <summary>Mirror-clones a repo into a bare repo, writes metadata.json, and packs both into one zip.</summary>
public sealed class GitArchiver
{
    private static readonly string[] MirrorRefspecs =
    {
        "+refs/heads/*:refs/heads/*",
        "+refs/tags/*:refs/tags/*"
    };

    /// <summary>Returns the path of the created &lt;owner&gt;__&lt;name&gt;.zip.</summary>
    public string Archive(RepoInfo info, string targetDir, string token)
    {
        Directory.CreateDirectory(targetDir);
        string staging = Path.Combine(Path.GetTempPath(), "gitclone-stage-" + Guid.NewGuid().ToString("N"));
        string bareDir = Path.Combine(staging, "repo.git");
        Directory.CreateDirectory(bareDir);

        try
        {
            MirrorClone(info.CloneUrl, bareDir, token);

            RepoStats stats;
            using (var repo = new Repository(bareDir))
                stats = GitStats.Compute(repo);

            var meta = new ArchiveMetadata
            {
                Owner = info.Owner,
                Name = info.Name,
                Description = info.Description,
                Private = info.Private,
                DefaultBranch = info.DefaultBranch,
                Topics = info.Topics,
                SourceUrl = info.CloneUrl,
                ArchivedAt = DateTimeOffset.UtcNow,
                RefCount = stats.RefCount,
                CommitCount = stats.CommitCount
            };
            File.WriteAllText(
                Path.Combine(staging, "metadata.json"),
                JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }));

            string zipPath = Path.Combine(targetDir, $"{info.Owner}__{info.Name}.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(staging, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            return zipPath;
        }
        finally
        {
            TryDeleteDir(staging);
        }
    }

    /// <summary>Re-opens the zip, extracts the mirror, recomputes stats and compares to metadata.</summary>
    public VerificationResult Verify(string zipPath)
    {
        if (!File.Exists(zipPath))
            return VerificationResult.Fail("Archiv-Datei fehlt.");

        string work = Path.Combine(Path.GetTempPath(), "gitclone-verify-" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(zipPath, work);

            string metaPath = Path.Combine(work, "metadata.json");
            if (!File.Exists(metaPath))
                return VerificationResult.Fail("metadata.json fehlt im Archiv.");

            var meta = JsonSerializer.Deserialize<ArchiveMetadata>(File.ReadAllText(metaPath));
            if (meta is null)
                return VerificationResult.Fail("metadata.json ist unlesbar.");

            string bareDir = Path.Combine(work, "repo.git");
            if (!Directory.Exists(bareDir))
                return VerificationResult.Fail("repo.git fehlt im Archiv.");

            RepoStats actual;
            using (var repo = new Repository(bareDir))
                actual = GitStats.Compute(repo);

            if (actual.RefCount != meta.RefCount || actual.CommitCount != meta.CommitCount)
                return VerificationResult.Fail(
                    $"Inhalt weicht ab: erwartet {meta.RefCount} Refs/{meta.CommitCount} Commits, " +
                    $"gefunden {actual.RefCount}/{actual.CommitCount}.");

            return VerificationResult.Success();
        }
        catch (Exception ex)
        {
            return VerificationResult.Fail("Verifikation fehlgeschlagen: " + ex.Message);
        }
        finally
        {
            TryDeleteDir(work);
        }
    }

    private static void MirrorClone(string url, string bareDir, string token)
    {
        Repository.Init(bareDir, isBare: true);
        using var repo = new Repository(bareDir);
        repo.Network.Remotes.Add("origin", url, "+refs/heads/*:refs/heads/*");

        var options = new FetchOptions { TagFetchMode = TagFetchMode.All };
        if (!string.IsNullOrEmpty(token) && url.StartsWith("http"))
            options.CredentialsProvider = (_, _, _) =>
                new UsernamePasswordCredentials { Username = token, Password = "" };

        Commands.Fetch(repo, "origin", MirrorRefspecs, options, null);
    }

    internal static void TryDeleteDir(string dir)
    {
        try
        {
            if (!Directory.Exists(dir)) return;
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(dir, recursive: true);
        }
        catch { /* best-effort */ }
    }
}
