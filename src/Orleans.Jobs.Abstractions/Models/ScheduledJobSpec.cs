using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models
{
    public class ScheduledJobSpec
    {
        public string TemplateId { get; set; } = string.Empty; // grain key
        public JobSpec Job { get; set; } = new JobSpec();      // job template to run on each fire

        // CRON
        public string Cron { get; set; } = "* * * * *"; // default every minute
        public string CronTimeZone { get; set; } = "UTC";
        public bool AllowOverlappingJobs { get; set; } = false;
        public int? MaxRuns { get; set; } = null;
        public DateTimeOffset? NotBefore { get; set; } = null;
        public DateTimeOffset? NotAfter { get; set; } = null;

        public ScheduledJobStatus Status { get; set; } = ScheduledJobStatus.Enabled;
    }
}
