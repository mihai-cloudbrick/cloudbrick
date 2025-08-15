using FluentAssertions;
using Xunit;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Orleans.Jobs.Tests;

public class CancelTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public CancelTests(ClusterFixture fx) { _fx = fx; }

    [Fact]
    public async Task CancelStopsTasks()
    {
        var mgr = _fx.Cluster.GrainFactory.GetGrain<IJobsManagerGrain>("manager");
        var spec = new JobSpec
        {
            Name = "cancel-job",
            MaxDegreeOfParallelism = 2,
            FailFast = false
        };
        spec.Tasks["t1"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1000, \"steps\": 10 }" };
        spec.Tasks["t2"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1000, \"steps\": 10 }" };

        var jobId = await mgr.CreateJobAsync(spec);
        await mgr.StartJobAsync(jobId);

        await Task.Delay(200);
        await mgr.CancelJobAsync(jobId);

        var state = await mgr.GetJobStateAsync(jobId);
        state.Should().NotBeNull();
        state!.Status.Should().BeOneOf(Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Cancelling, Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Cancelled);
    }
}
