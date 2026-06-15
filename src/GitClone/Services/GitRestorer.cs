using System.IO.Compression;
using GitClone.Models;
using LibGit2Sharp;

namespace GitClone.Services;

/// <summary>Extracts an archive's bare mirror and pushes every branch and tag to a target remote.</summary>
public sealed class GitRestorer
{
    private static readonly string[] PushRefspecs =
    {
        "+refs/heads/*:refs/heads/*",
        "+refs/tags/*:refs/tags/*"
    };

    /// <summary>Push the mirror inside <paramref name="zipPath"/> to <paramref name="remoteUrl"/>.</summary>
    public void PushArchiveTo(string zipPath, string remoteUrl, string token)
    {
        string work = Path.Combine(Path.GetTempPath(), "gitclone-restore-" + Guid.NewGuid().ToString("N"));
        try
        {
            ZipFile.ExtractToDirectory(zipPath, work);
            string bareDir = Path.Combine(work, "repo.git");

            using var repo = new Repository(bareDir);
            var remote = repo.Network.Remotes.Add("target", remoteUrl);

            var options = new PushOptions();
            if (!string.IsNullOrEmpty(token) && remoteUrl.StartsWith("http"))
                options.CredentialsProvider = (_, _, _) =>
                    new UsernamePasswordCredentials { Username = token, Password = "" };

            repo.Network.Push(remote, PushRefspecs, options);
        }
        finally
        {
            GitArchiver.TryDeleteDir(work);
        }
    }

    /// <summary>Read metadata without extracting the whole archive (used by the UI to create the repo first).</summary>
    public ArchiveMetadata ReadMetadata(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var entry = zip.GetEntry("metadata.json")
                    ?? throw new InvalidOperationException("metadata.json fehlt im Archiv.");
        using var reader = new StreamReader(entry.Open());
        return System.Text.Json.JsonSerializer.Deserialize<ArchiveMetadata>(reader.ReadToEnd())
               ?? throw new InvalidOperationException("metadata.json ist unlesbar.");
    }
}
