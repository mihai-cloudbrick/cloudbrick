using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Xunit;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Orleans.Jobs.Tests;

public class RetryTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public RetryTests(ClusterFixture fx) { _fx = fx; }

    [Fact]
    public async Task RetryHappens()
    {
        var mgr = _fx.Cluster.GrainFactory.GetGrain<IJobsManagerGrain>("manager");
        var spec = new JobSpec
        {
            Name = "retry-job",
            MaxDegreeOfParallelism = 1,
            FailFast = true
        };
        // Use delay executor with short backoff; just ensure it runs
        spec.Tasks["t1"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 10, \"steps\": 2 }", MaxRetries = 1, RetryBackoffSeconds = 1 };

        var jobId = await mgr.CreateJobAsync(spec);
        await mgr.StartJobAsync(jobId);

        JobState? state;
        int guard = 0;
        do
        {
            await Task.Delay(50);
            state = await mgr.GetJobStateAsync(jobId);
            state.Should().NotBeNull();
            guard++;
            if (guard > 200) break;
        } while (state!.Status != Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Succeeded);

        state!.Tasks["t1"].Attempts.Should().BeGreaterThan(0);
        state.Status.Should().Be(Cloudbrick.Orleans.Jobs.Abstractions.Enums.JobStatus.Succeeded);
    }
}
