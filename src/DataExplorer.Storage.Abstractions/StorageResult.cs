#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Threading;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class StorageResult<T>
{
    public IExecutionContext? Context { get; set; }
    public OperationStatus Status { get; set; } = OperationStatus.None;
    public string? ETag { get; set; }

    public T? Item { get; set; }
    public IReadOnlyList<T>? Items { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public TimeSpan Duration { get; set; }

    public Exception? Error { get; set; }
    public IReadOnlyList<LogEntry> Logs { get; set; } = Array.Empty<LogEntry>();
    public IReadOnlyDictionary<string, ChangeRecord>? Changes { get; set; }
}
