#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

#nullable enable


namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
public sealed record EncryptionOptions
{
    public bool Enabled { get; init; } = false;
    public string? KeyBase64 { get; init; }
    public string KeyId { get; init; } = "default";
}
