using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Enums
{
    public enum ScheduledJobStatus
    {
        Disabled = 0,
        Enabled = 1,
        Paused = 2
    }
}
