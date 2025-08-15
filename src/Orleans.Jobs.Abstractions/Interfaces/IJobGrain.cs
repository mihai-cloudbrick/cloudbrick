using System;
using System.Threading.Tasks;
using Orleans;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface IJobGrain : IGrainWithGuidKey
{
    Task SubmitAsync(JobSpec spec);
    Task StartAsync();
    Task PauseAsync();
    Task ResumeAsync();
    Task CancelAsync();
    Task DeleteAsync();
    Task<JobState?> GetStateAsync();
    Task FlushAsync();
    Task EmitTelemetryAsync(ExecutionEvent evt);
    Task SetTelemetryProviderAsync(string providerKey);
}
