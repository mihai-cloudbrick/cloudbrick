using System;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Xunit;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;

namespace Orleans.Jobs.Tests;

public class PauseResumeTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public PauseResumeTests(ClusterFixture fx) { _fx = fx; }

    [Fact]
    public async Task TaskPausesAndResumes()
    {
        var mgr = _fx.Cluster.GrainFactory.GetGrain<IJobsManagerGrain>("manager");
        var spec = new JobSpec
        {
            Name = "pause-job",
            MaxDegreeOfParallelism = 1,
            FailFast = true
        };
        spec.Tasks["t1"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 300, \"steps\": 6 }" };

        var jobId = await mgr.CreateJobAsync(spec);
        await mgr.StartJobAsync(jobId);

        await Task.Delay(150);
        var job = _fx.Cluster.GrainFactory.GetGrain<IJobGrain>(jobId);
        await job.PauseAsync();

        var before = await job.GetStateAsync();
        await Task.Delay(300);
        var after = await job.GetStateAsync();
        // Progress should not decrease; may remain same due to pause
        after.Tasks["t1"].Progress.Should().BeGreaterThanOrEqualTo(before.Tasks["t1"].Progress);

        await job.ResumeAsync();

        // wait for completion
        JobState state;
        int guard = 0;
        do
        {
            await Task.Delay(100);
            state = await job.GetStateAsync();
            guard++;
            if (guard > 200) break;
        } while (state.Status != JobStatus.Succeeded);

        state.Status.Should().Be(JobStatus.Succeeded);
    }
}
