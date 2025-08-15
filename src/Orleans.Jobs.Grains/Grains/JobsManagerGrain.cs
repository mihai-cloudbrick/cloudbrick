using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Grains;

internal class JobsManagerGrain : Grain, IJobsManagerGrain
{
    private readonly IPersistentState<List<Guid>> _jobs;

    public JobsManagerGrain([PersistentState(stateName: "jobs", storageName: "Default")] IPersistentState<List<Guid>> jobs)
    {
        _jobs = jobs;
    }

    public async Task<Guid> CreateJobAsync(JobSpec spec)
    {
        var id = Guid.NewGuid();
        var job = GrainFactory.GetGrain<IJobGrain>(id);
        await job.SubmitAsync(spec);
        _jobs.State ??= new List<Guid>();
        _jobs.State.Add(id);
        await _jobs.WriteStateAsync();
        return id;
    }

    public Task SetJobTelemetryProviderAsync(Guid jobId, string providerKey) =>
        GrainFactory.GetGrain<IJobGrain>(jobId).SetTelemetryProviderAsync(providerKey);

    public Task StartJobAsync(Guid jobId) => GrainFactory.GetGrain<IJobGrain>(jobId).StartAsync();
    public Task PauseJobAsync(Guid jobId) => GrainFactory.GetGrain<IJobGrain>(jobId).PauseAsync();
    public Task ResumeJobAsync(Guid jobId) => GrainFactory.GetGrain<IJobGrain>(jobId).ResumeAsync();
    public Task CancelJobAsync(Guid jobId) => GrainFactory.GetGrain<IJobGrain>(jobId).CancelAsync();
    public async Task DeleteJobAsync(Guid jobId)
    {
        var job = GrainFactory.GetGrain<IJobGrain>(jobId);
        if (_jobs.State != null && _jobs.State.Remove(jobId))
        {
            await _jobs.WriteStateAsync();
        }
        await job.DeleteAsync();
    }
    public Task<JobState?> GetJobStateAsync(Guid jobId) => GrainFactory.GetGrain<IJobGrain>(jobId).GetStateAsync();
    public Task<List<Guid>> ListJobsAsync() => Task.FromResult(_jobs.State ?? new List<Guid>());
}
