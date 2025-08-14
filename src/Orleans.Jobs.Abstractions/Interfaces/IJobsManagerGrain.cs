using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface IJobsManagerGrain : IGrainWithStringKey
{
    Task<Guid> CreateJobAsync(JobSpec spec);
    Task SetJobTelemetryProviderAsync(Guid jobId, string providerKey);
    Task StartJobAsync(Guid jobId);
    Task PauseJobAsync(Guid jobId);
    Task ResumeJobAsync(Guid jobId);
    Task CancelJobAsync(Guid jobId);
    Task<JobState> GetJobStateAsync(Guid jobId);
    Task<List<Guid>> ListJobsAsync();
}
