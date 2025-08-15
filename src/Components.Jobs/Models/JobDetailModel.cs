using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using System;
using System.Collections.Generic;


namespace Cloudbrick.Components.Jobs.Models
{
    public class JobDetailModel
    {
        public Guid JobId { get; set; }
        public JobStatus Status { get; set; }
        public string? CorrelationId { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public Dictionary<string, TaskSummary> Tasks { get; set; } = new Dictionary<string, TaskSummary>();

        // Aggregates
        public int TotalTasks { get; set; }
        public int RunningTasks { get; set; }
        public int QueuedTasks { get; set; }
        public int SucceededTasks { get; set; }
        public int FailedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int JobProgress { get; set; }
    }
}
