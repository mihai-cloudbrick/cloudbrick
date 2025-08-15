using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using System;

namespace Cloudbrick.Components.Jobs.Models
{
    public class ExecutionEventModel
    {
        public ExecutionEventType EventType { get; set; }
        public Guid JobId { get; set; }
        public string? TaskId { get; set; }
        public string? Message { get; set; }
        public int? Progress { get; set; }
        public string? Exception { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        // Aggregates (JobSnapshot)
        public int? TotalTasks { get; set; }
        public int? RunningTasks { get; set; }
        public int? QueuedTasks { get; set; }
        public int? SucceededTasks { get; set; }
        public int? FailedTasks { get; set; }
        public int? CancelledTasks { get; set; }
        public int? CompletedTasks { get; set; }
        public int? JobProgress { get; set; }

        // Recurring
        public int? RunNumber { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
    }
}
