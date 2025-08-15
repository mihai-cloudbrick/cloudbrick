using System;
using System.Collections.Generic;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models;

public class JobState
{
    public Guid JobId { get; set; }
    public string Name { get; set; } = "job";
    public JobStatus Status { get; set; } = JobStatus.Created;
    public Dictionary<string, TaskState> Tasks { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public bool FailFast { get; set; } = true;
    public string CorrelationId { get; set; } = string.Empty;
    public string? TelemetryProviderKey { get; set; }
    public int TotalTasks { get; set; }
    public int RunningTasks { get; set; }
    public int QueuedTasks { get; set; }
    public int SucceededTasks { get; set; }
    public int FailedTasks { get; set; }
    public int CancelledTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int JobProgress { get; set; } // 0–100, based on completed/total

    public JobState Clone()
    {
        return new JobState
        {
            StartedAt = StartedAt,
            Status  = Status,
            SucceededTasks = SucceededTasks,
            CompletedTasks = CompletedTasks,
            JobProgress = JobProgress,
            CancelledTasks = CancelledTasks,
            CompletedAt = CompletedAt,
            CorrelationId = CorrelationId,
            CreatedAt = CreatedAt,
            FailedTasks = FailedTasks,
            FailFast = FailFast,
            JobId = JobId,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            Name = Name,
            QueuedTasks = QueuedTasks,
            RunningTasks = RunningTasks,
            Tasks = Tasks.ToDictionary(c=>c.Key, c => c.Value.Clone()),
            TelemetryProviderKey = TelemetryProviderKey,
            TotalTasks = TotalTasks,
        };
    }
}
