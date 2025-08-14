#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Threading;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class StorageQueryResult
{
    public IExecutionContext? Context { get; set; }
    public OperationStatus Status { get; set; } = OperationStatus.Unchanged; // queries don't mutate
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan Duration { get; set; }
    public Exception? Error { get; set; }
    public IReadOnlyList<LogEntry> Logs { get; set; } = Array.Empty<LogEntry>();
    public QueryPage Page { get; set; } = new();
}
