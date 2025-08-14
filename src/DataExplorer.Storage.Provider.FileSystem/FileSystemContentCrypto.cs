#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
internal static class FileSystemContentCrypto
{
    public static IContentEncryptor CreateEncryptor(FileSystemOptions options)
    {
        if (options.Encryption?.Enabled != true) return NoopEncryptor.Instance;
        if (string.IsNullOrWhiteSpace(options.Encryption!.KeyBase64))
            throw new InvalidOperationException("FileSystemOptions.Encryption.Enabled is true, but KeyBase64 is not set.");
        var key = Convert.FromBase64String(options.Encryption.KeyBase64!);
        return new AesGcmEncryptor(key);
    }
    public static ReadOnlySpan<byte> MakeAad(string databaseId, string tableId, string id)
        => System.Text.Encoding.UTF8.GetBytes($"{databaseId}|{tableId}|{id}");
}
