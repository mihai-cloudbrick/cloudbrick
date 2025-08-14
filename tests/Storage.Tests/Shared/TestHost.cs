#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Configuration;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using Cloudbrick.DataExplorer.Storage.Provider.AzureTable;
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Cloudbrick.DataExplorer.Storage.Provider.Sql;

namespace Cloudbrick.DataExplorer.Storage.Tests.Shared;

public static class TestHost
{
    public static ServiceProvider BuildBaseServices(Action<IServiceCollection>? more = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        }));

        services.AddResourceManagerStorageCore();

        // Register EVERY builder (tests pick what they use).
        services.AddSingleton<IStorageProviderBuilder, FileSystemProviderBuilder>();
        services.AddSingleton<IStorageProviderBuilder, AzureBlobProviderBuilder>();
        services.AddSingleton<IStorageProviderBuilder, AzureTableProviderBuilder>();
        services.AddSingleton<IStorageProviderBuilder, CosmosProviderBuilder>();
        services.AddSingleton<IStorageProviderBuilder, SqlProviderBuilder>();

        more?.Invoke(services);
        return services.BuildServiceProvider(validateScopes: true) as ServiceProvider;
    }

    public static IDisposable PushContext(ServiceProvider sp, string action = "Test")
    {
        var accessor = sp.GetRequiredService<DefaultExecutionContextAccessor>();
        return accessor.Push(new ExecutionContext
        {
            ActionName = action,
            TrackingId = Guid.NewGuid().ToString("N"),
            PrincipalId = "test-user"
        });
    }

    public static async Task<IConfigAwareStorageManager> BuildManager(ServiceProvider sp, bool createStructures = true)
    {
        var manager = sp.GetRequiredService<IConfigAwareStorageManager>();
        await manager.InitializeAsync(createStructures);        
        return manager;
    }
}
