using System;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models;

public class ExecutionEvent
{
    public ExecutionEventType EventType { get; set; }
    public Guid JobId { get; set; }
    public string TaskId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? Progress { get; set; }
    public string? Exception { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string CorrelationId { get; set; } = string.Empty;

    // NEW: job-level aggregates (used when TaskId is null/empty)
    public int? TotalTasks { get; set; }
    public int? RunningTasks { get; set; }
    public int? QueuedTasks { get; set; }
    public int? SucceededTasks { get; set; }
    public int? FailedTasks { get; set; }
    public int? CancelledTasks { get; set; }
    public int? CompletedTasks { get; set; }
    public int? JobProgress { get; set; }
    // NEW — per-run metadata for recurring tasks
    public int? RunNumber { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
}
