#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class QueryPage
{
    public IReadOnlyList<JsonObject> Rows { get; set; } = Array.Empty<JsonObject>();
    public string? ContinuationToken { get; set; }
    public QueryStatistics? Stats { get; set; }
}
