using System.IO.Compression;
using System.Text.Json;
using GitClone.Models;

namespace GitClone.Services;

public sealed record ArchiveEntry(string ZipPath, ArchiveMetadata Metadata);

/// <summary>Scans a target folder and reads metadata.json out of each archive zip (no full extract).</summary>
public sealed class ArchiveIndex
{
    public IReadOnlyList<ArchiveEntry> List(string folder)
    {
        var result = new List<ArchiveEntry>();
        if (!Directory.Exists(folder)) return result;

        foreach (string zipPath in Directory.EnumerateFiles(folder, "*.zip"))
        {
            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                var entry = zip.GetEntry("metadata.json");
                if (entry is null) continue;

                using var reader = new StreamReader(entry.Open());
                var meta = JsonSerializer.Deserialize<ArchiveMetadata>(reader.ReadToEnd());
                if (meta is not null)
                    result.Add(new ArchiveEntry(zipPath, meta));
            }
            catch
            {
                // Not a GitClone archive — skip silently.
            }
        }

        return result.OrderBy(e => e.Metadata.Name).ToList();
    }
}
