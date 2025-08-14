#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class StorageItem
{
    public string Id { get; set; } = default!;
    public JsonObject Data { get; set; } = new();
    public string? ETag { get; set; } = "";
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedUtc { get; set; } = default!;
}
