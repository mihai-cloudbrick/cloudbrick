#nullable enable

using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

internal static class ShardedPathBuilder
{
    public static string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "_";
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }

    /// <summary>
    /// Returns the final file path (including .json) for an item id using SHA-256 sharding.
    /// Does not create directories.
    /// </summary>
    public static string BuildItemPath(FileSystemOptions opt, string root, string databaseId, string tableId, string itemId)
    {
        var db = Sanitize(databaseId);
        var table = Sanitize(tableId);
        var hash = FileNameHasher.Sha256Hex(itemId);

        var parts = new List<string>
        {
            root,
            db,
            table
        };

        var idx = 0;
        for (var level = 0; level < opt.ShardDepth; level++)
        {
            var take = Math.Min(opt.ShardWidth, hash.Length - idx);
            if (take <= 0) break;
            parts.Add(hash.Substring(idx, take));
            idx += take;
        }

        parts.Add(hash + ".json");
        return Path.Combine(parts.ToArray());
    }

    /// <summary>Create parent directory for a file path if missing.</summary>
    public static void EnsureParentDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}
