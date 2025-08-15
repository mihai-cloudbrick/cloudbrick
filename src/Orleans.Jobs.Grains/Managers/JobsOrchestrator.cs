using Cloudbrick.Orleans.Jobs.Abstractions.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Managers
{
    public class JobsOrchestrator
    {
        public JobsOrchestrator(IJobsManager jobs, IScheduledJobsManager scheduledJobs)
        {
            Jobs = jobs;
            ScheduledJobs = scheduledJobs;
        }

        public IJobsManager Jobs { get; }
        public IScheduledJobsManager ScheduledJobs { get; }
    }
}
