#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class ChangeRecord
{
    public JsonNode? OldValue { get; set; }
    public JsonNode? NewValue { get; set; }
    public string PrincipalId { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
}
