using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models
{
    public class ScheduledJobState
    {
        public string TemplateId { get; set; } = string.Empty;
        public ScheduledJobStatus Status { get; set; } = ScheduledJobStatus.Disabled;

        public string Cron { get; set; } = "* * * * *";
        public string CronTimeZone { get; set; } = "UTC";
        public bool AllowOverlappingJobs { get; set; } = false;
        public int? MaxRuns { get; set; }
        public int RunCount { get; set; }
        public DateTimeOffset? NotBefore { get; set; }
        public DateTimeOffset? NotAfter { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastRunAt { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }

        public Guid? LastJobId { get; set; }
        public List<Guid> RecentJobIds { get; set; } = new List<Guid>();

        public JobSpec Job { get; set; } = new JobSpec();
    }
}
