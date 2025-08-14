#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Provider.Cosmos;

public sealed class CosmosOptions : ProviderOptionsBase
{
    public override StorageProviderKind Kind => StorageProviderKind.CosmosDb;

    public required string Endpoint { get; set; }
    public required string Key { get; set; }

    /// <summary>Default partition key path for new containers. Default: /id.</summary>
    public string PartitionKeyPath { get; set; } = "/id";

    /// <summary>Throughput for newly created containers (manual RU/s). Default: 400. Set to null to use database defaults.</summary>
    public int? DefaultThroughput { get; set; } = 400;

    /// <summary>Optional Cosmos consistency level as string (e.g., "Session"). If null, SDK default applies.</summary>
    public string? Consistency { get; set; }
}
