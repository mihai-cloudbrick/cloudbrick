using System;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Xunit;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Orleans.Jobs.Tests;

public class JobDagTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public JobDagTests(ClusterFixture fx) { _fx = fx; }

    [Fact]
    public async Task SimpleJobRunsToCompletion()
    {
        var mgr = _fx.Cluster.GrainFactory.GetGrain<IJobsManagerGrain>("manager");
        var spec = new JobSpec
        {
            Name = "adder-job",
            MaxDegreeOfParallelism = 2,
            FailFast = true,
            CorrelationId = Guid.NewGuid().ToString("N"),
            TelemetryProviderKey = "console"
        };
        spec.Tasks["t1"] = new TaskSpec { ExecutorType = "adder", CommandJson = "{ \"a\": 2, \"b\": 3 }" };
        spec.Tasks["t2"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 50, \"steps\": 3 }", Dependencies = { "t1" } };

        var jobId = await mgr.CreateJobAsync(spec);
        await mgr.StartJobAsync(jobId);

        JobState state;
        int guard = 0;
        do
        {
            await Task.Delay(100);
            state = await mgr.GetJobStateAsync(jobId);
            guard++;
            if (guard > 200) break;
        } while (state.Status != Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Succeeded &&
                 state.Status != Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Failed);

        state.Status.Should().Be(Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Succeeded);
    }
}
