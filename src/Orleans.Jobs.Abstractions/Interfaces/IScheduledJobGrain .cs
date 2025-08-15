using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces
{
    public interface IScheduledJobGrain : IGrainWithStringKey
    {
        Task ConfigureAsync(ScheduledJobSpec spec);
        [AlwaysInterleave] Task EnableAsync();
        [AlwaysInterleave] Task DisableAsync();
        [AlwaysInterleave] Task PauseAsync();
        [AlwaysInterleave] Task ResumeAsync();
        [AlwaysInterleave] Task RunNowAsync();
        Task<ScheduledJobState> GetStateAsync();
        Task UpdateSpecAsync(ScheduledJobSpec spec);
        Task DeleteAsync();
    }
}
