#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

public sealed class BlobOptions : ProviderOptionsBase
{
    public override StorageProviderKind Kind => StorageProviderKind.AzureBlobStorage;

    public required string ConnectionString { get; set; }

    // Sharding for JSON blobs beneath a table prefix
    public int ShardDepth { get; set; } = 2;   // number of directories
    public int ShardWidth { get; set; } = 2;   // characters per shard
}
