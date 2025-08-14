using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Cloudbrick.Orleans.Jobs.Abstractions;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Executors;
using Cloudbrick.Orleans.Jobs.Telemetry;

namespace Orleans.Jobs.Tests;

public class ClusterFixture : IDisposable
{
    public TestCluster Cluster { get; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloConfig>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose() => Cluster.Dispose();

    private class SiloConfig : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .AddMemoryGrainStorageAsDefault()
                    .AddMemoryStreams(StreamConstants.ProviderName)
                    .AddMemoryGrainStorage(StreamConstants.ProviderName); ;

            siloBuilder.Services
                .AddSingleton<ITaskExecutor, DelayExecutor>()
                .AddSingleton<ITaskExecutor, AdderExecutor>()
                .AddSingleton<ITaskExecutorFactory, ExecutorFactory>()
                .AddSingleton<ITelemetrySinkFactory, TelemetrySinkFactory>();
        }
    }
}
