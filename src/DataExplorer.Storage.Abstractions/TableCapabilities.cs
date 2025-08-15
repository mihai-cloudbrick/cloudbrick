#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class TableCapabilities
{
    public TableCapabilityFlags Flags { get; set; } = TableCapabilityFlags.None;
    public int? MaxPageSize { get; set; }
    public string ProviderName { get; set; } = string.Empty; // e.g., "FileSystem", "AzureBlob", "Cosmos", "Sql", "AzureTable"
    public string Version { get; set; } = "1.0";
}
