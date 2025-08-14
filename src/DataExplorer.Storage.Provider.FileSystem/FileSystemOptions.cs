#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

public sealed class FileSystemOptions : ProviderOptionsBase
{
    public override StorageProviderKind Kind => StorageProviderKind.LocalFileSystem;

    /// <summary>Root folder under which all databases/tables live.</summary>
    public required string Root { get; set; }

    /// <summary>Number of shard levels (directories) to create from the hash prefix. Default: 2.</summary>
    public int ShardDepth { get; set; } = 2;

    /// <summary>Characters per shard directory (from the hash). Default: 2.</summary>
    public int ShardWidth { get; set; } = 2;
    public EncryptionOptions Encryption { get; set; }
}
