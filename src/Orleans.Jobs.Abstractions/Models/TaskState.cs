using System;
using System.Collections.Generic;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models;

public class TaskState
{
    public string TaskId { get; set; } = string.Empty;
    public string ExecutorType { get; set; } = string.Empty;
    public string CommandJson { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public JobTaskStatus Status { get; set; } = JobTaskStatus.Created;
    public int Attempts { get; set; } = 0;
    public int Progress { get; set; } = 0;
    public string? OutputJson { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<TaskHistoryEntry> History { get; set; } = new();
    public string JobId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    // NEW — CRON runtime state
    public bool IsRecurring { get; set; }
    public string? Cron { get; set; }
    public string? CronTimeZone { get; set; }
    public bool AllowConcurrentRuns { get; set; }
    public int? MaxRuns { get; set; }
    public int RunCount { get; set; }
    public DateTimeOffset? NotBefore { get; set; }
    public DateTimeOffset? NotAfter { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
    public DateTimeOffset? NextRunAt { get; set; }
    public JobTaskStatus? PausedFrom { get; set; }
}
