#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cloudbrick.Orleans.Persistance.DataExplorer;

namespace Cloudbricks.Orleans.Tests;

public class GrainStorageMappingTests
{
    [Fact]
    public void Mapper_ComposesIds_AsExpected()
    {
        var (sp, _) = TestHost.Build();
        var mgr = sp.GetRequiredService<IConfigAwareStorageManager>();
        var opts = Options.Create(new DataExplorerOrleansStorageOptions { DefaultDatabaseId = "orleans-db" });
        var logger = sp.GetRequiredService<ILogger<DataExplorerGrainStorage>>();
        var provider = new DataExplorerGrainStorage("Default", mgr, new OptionsMonitorStub<DataExplorerOrleansStorageOptions>(opts.Value), logger);

        var (db, table, id) = provider.MapForTest("My.GrainType", "user-123", "eu");
        db.Should().Be("orleans-db");
        table.Should().Be("My.GrainType");
        id.Should().Be("user-123|eu");
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
    {
        private readonly T _value;
        public OptionsMonitorStub(T value) => _value = value;
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
