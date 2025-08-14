#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureTable;

/// <summary>
/// Options for Azure Table provider. A "database" is modeled as a table name prefix.
/// Physical table name: <DbPrefix><Separator><SanitizedTableId>.
/// </summary>
public sealed class TableOptions : ProviderOptionsBase
{
    public override StorageProviderKind Kind => StorageProviderKind.AzureTableStorage;

    public required string ConnectionString { get; set; }

    /// <summary>When true, prepend a sanitized databaseId + Separator to the physical table name.</summary>
    public bool UseDatabasePrefix { get; set; } = true;

    /// <summary>Separator between db prefix and table id in the physical name.</summary>
    public string Separator { get; set; } = "";

    /// <summary>Partition strategy: number of chars taken from SHA256(Id) for partition key. Default 2.</summary>
    public int PartitionPrefixLength { get; set; } = 2;

    /// <summary>Whether to store RowKey as original Id (true) or full SHA256(Id) (false).</summary>
    public bool RowKeyIsOriginalId { get; set; } = true;
}
