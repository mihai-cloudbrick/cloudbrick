using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Managers
{
    public interface IJobsManager
    {
        Task CancelAsync(Guid id);
        Task<Guid> CreateAsync(JobSpec spec);
        Task<JobDetail?> GetAsync(Guid id);
        Task<List<JobSummary>> ListAsync();
        Task PauseAsync(Guid id);
        Task ResumeAsync(Guid id);
        Task StartAsync(Guid id);
    }
}