using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Managers
{
    /// <summary>
    /// Manager for creating and controlling scheduled jobs. All mutating
    /// operations return the manager instance to allow fluent chaining.
    /// </summary>
    public interface IScheduledJobsManager
    {
        /// <summary>
        /// Creates a new scheduled job and returns the manager for chaining along
        /// with the generated identifier.
        /// </summary>
        Task<(IScheduledJobsManager Manager, string Id)> CreateAsync(ScheduledJobSpec spec);

        /// <summary>Deletes the specified scheduled job.</summary>
        Task<IScheduledJobsManager> DeleteAsync(string id);

        /// <summary>Disables the specified scheduled job.</summary>
        Task<IScheduledJobsManager> DisableAsync(string id);

        /// <summary>Enables the specified scheduled job.</summary>
        Task<IScheduledJobsManager> EnableAsync(string id);

        /// <summary>Gets the current state of the scheduled job.</summary>
        Task<ScheduledJobState?> GetAsync(string id);

        /// <summary>Lists all known scheduled job identifiers.</summary>
        Task<List<string>> ListAsync();

        /// <summary>Pauses the specified scheduled job.</summary>
        Task<IScheduledJobsManager> PauseAsync(string id);

        /// <summary>Resumes a previously paused scheduled job.</summary>
        Task<IScheduledJobsManager> ResumeAsync(string id);

        /// <summary>Runs the scheduled job immediately.</summary>
        Task<IScheduledJobsManager> RunNowAsync(string id);

        /// <summary>Updates an existing scheduled job.</summary>
        Task<IScheduledJobsManager> UpdateAsync(string id, ScheduledJobSpec spec);
    }
}