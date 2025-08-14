#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using System.Security.Cryptography;
using System.Text;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

internal static class FileNameHasher
{
    public static string Sha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
