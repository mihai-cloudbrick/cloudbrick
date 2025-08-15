using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models
{
    public class TaskSummary
    {
        public string TaskId { get; set; } = string.Empty;
        public string ExecutorType { get; set; } = string.Empty;
        public JobTaskStatus Status { get; set; } // enum numeric
        public int Progress { get; set; }
        public string? LastError { get; set; }
        public int RunCount { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
