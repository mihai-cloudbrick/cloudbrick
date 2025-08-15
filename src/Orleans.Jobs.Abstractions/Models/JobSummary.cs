using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models
{
    public class JobSummary
    {
        public Guid JobId { get; set; }
        public JobStatus Status { get; set; } // enum numeric
        public string? CorrelationId { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public int JobProgress { get; set; }
    }
}
