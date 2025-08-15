using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Managers;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cloudbrick.Orleans.Jobs.Managers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Orleans.Jobs.Tests;

public class FluentScheduledJobsTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fx;
    public FluentScheduledJobsTests(ClusterFixture fx) => _fx = fx;

    [Fact]
    public async Task ManagerSupportsFluentChaining()
    {
        var manager = new ScheduledJobsManager(_fx.Cluster.Client, NullLogger<ScheduledJobsManager>.Instance);
        var spec = new ScheduledJobSpec
        {
            Job = new JobSpec()
        };
        spec.Job.Tasks["t1"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1 }" };

        var creation = await manager.CreateAsync(spec);

        var chained = await (await (await (await creation.Manager.DisableAsync(creation.Id))
            .EnableAsync(creation.Id))
            .PauseAsync(creation.Id))
            .ResumeAsync(creation.Id);

        await chained.RunNowAsync(creation.Id);
        await chained.DeleteAsync(creation.Id);
    }
}
