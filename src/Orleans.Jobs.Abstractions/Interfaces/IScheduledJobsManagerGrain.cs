using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces
{
    public interface IScheduledJobsManagerGrain : IGrainWithStringKey
    {
        Task<string> CreateAsync(ScheduledJobSpec spec);
        Task UpdateAsync(ScheduledJobSpec spec);
        Task<ScheduledJobState> GetAsync(string templateId);
        Task<List<string>> ListAsync();
        Task DeleteAsync(string templateId);

        Task EnableAsync(string templateId);
        Task DisableAsync(string templateId);
        Task PauseAsync(string templateId);
        Task ResumeAsync(string templateId);
        Task RunNowAsync(string templateId);
    }
}
