using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Orleans;
using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface ITaskGrain : IGrainWithStringKey
{
    Task StartAsync(Guid jobId, TaskSpec spec);
    Task PauseAsync();   // <—
    Task ResumeAsync();  // <—
    Task CancelAsync();  // <—
    Task<TaskState> GetStateAsync();
    Task FlushAsync();
    Task EmitTelemetryAsync(ExecutionEvent evt);
}
