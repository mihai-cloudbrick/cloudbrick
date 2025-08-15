using System;
using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using FluentAssertions;
using Xunit;

namespace Orleans.Jobs.Tests;

public class DeleteTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public DeleteTests(ClusterFixture fx) { _fx = fx; }

    [Fact]
    public async Task DeleteRemovesJob()
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

        var ids = await mgr.ListJobsAsync();
        ids.Should().Contain(jobId);

        await mgr.DeleteJobAsync(jobId);
        var after = await mgr.ListJobsAsync();
        after.Should().NotContain(jobId);
    }
}
