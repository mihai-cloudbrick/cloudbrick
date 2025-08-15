using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Managers
{
    public interface IScheduledJobsManager
    {
        Task<string> CreateAsync(ScheduledJobSpec spec);
        Task DeleteAsync(string id);
        Task DisableAsync(string id);
        Task EnableAsync(string id);
        Task<ScheduledJobState?> GetAsync(string id);
        Task<List<string>> ListAsync();
        Task PauseAsync(string id);
        Task ResumeAsync(string id);
        Task RunNowAsync(string id);
        Task UpdateAsync(string id, ScheduledJobSpec spec);
    }
}