using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models
{
    public class JobDetail
    {
        public Guid JobId { get; set; }
        public JobStatus Status { get; set; } // enum numeric
        public string? CorrelationId { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public Dictionary<string, TaskSummary> Tasks { get; set; } = new();

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
