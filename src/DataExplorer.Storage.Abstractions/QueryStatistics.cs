#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class QueryStatistics
{
    public TimeSpan Elapsed { get; set; }
    public long? ScannedItems { get; set; }
    public long? ScannedBytes { get; set; }
    public double? RequestUnits { get; set; } // Cosmos, etc.
}
