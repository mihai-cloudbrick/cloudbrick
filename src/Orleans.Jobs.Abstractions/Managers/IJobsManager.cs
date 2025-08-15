using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Managers
{
    public interface IJobsManager
    {
        Task<IJobsManager> CancelAsync(Guid id);
        Task<Guid> CreateAsync(JobSpec spec);
        Task<JobDetail?> GetAsync(Guid id);
        Task<List<JobSummary>> ListAsync();
        Task<IJobsManager> PauseAsync(Guid id);
        Task<IJobsManager> ResumeAsync(Guid id);
        Task<IJobsManager> StartAsync(Guid id);
        Task<IJobsManager> DeleteAsync(Guid id);
    }
}