#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class QuerySpec
{
    public QueryLanguage Language { get; set; } = QueryLanguage.KustoLoco;
    public string Text { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, JsonNode?>? Parameters { get; set; }
    public int? PageSize { get; set; }
    public string? ContinuationToken { get; set; }
    public TimeSpan? Timeout { get; set; }
}
