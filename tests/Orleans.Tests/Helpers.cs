#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cloudbricks.Orleans.Tests;

internal static class TestHost
{
    public static (ServiceProvider SP, string Root) Build()
    {
        var root = Path.Combine(Path.GetTempPath(), "rm-orleans-tests", Guid.NewGuid().ToString("N"));
        var sc = new ServiceCollection();
        sc.AddLogging(b => b.AddDebug().AddConsole());
        sc.AddResourceManagerStorageCore();
        sc.AddSingleton<IStorageProviderBuilder, FileSystemProviderBuilder>();
        sc.AddResourceManagerStorageDatabase("orleans-db", () => new FileSystemOptions { Root = root, ShardDepth = 2, ShardWidth = 2 });
        return (sc.BuildServiceProvider(), root);
    }
}
