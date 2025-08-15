using FluentAssertions;
using Xunit;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Orleans.Jobs.Tests;

public class DeleteTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public DeleteTests(ClusterFixture fx) { _fx = fx; }

    [Fact]
    public async Task DeletedJobNotListedOrRetrievable()
    {
        var mgr = _fx.Cluster.GrainFactory.GetGrain<IJobsManagerGrain>("manager");
        var spec = new JobSpec
        {
            Name = "delete-job",
            MaxDegreeOfParallelism = 1,
            FailFast = true
        };
        spec.Tasks["t1"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 10, \"steps\": 1 }" };

        var jobId = await mgr.CreateJobAsync(spec);
        await mgr.StartJobAsync(jobId);
        await mgr.DeleteJobAsync(jobId);

        var list = await mgr.ListJobsAsync();
        list.Should().NotContain(jobId);

        var state = await mgr.GetJobStateAsync(jobId);
        state.Should().BeNull();
    }
}
