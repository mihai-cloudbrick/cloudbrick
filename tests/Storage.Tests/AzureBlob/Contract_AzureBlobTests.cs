#nullable enable
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using Cloudbrick.DataExplorer.Storage.Tests.Shared;
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Tests.AzureBlob;

public class Contract_AzureBlobTests
{
    private static (ServiceProvider sp, string dbId) BuildOrSkip()
    {
        var conn = Constants.AzureStorageConnectionString;

        Skip.If(string.IsNullOrWhiteSpace(conn), "Set TEST_BLOB_CONN to run Azure Blob tests (Azurite or real).");

        var dbId = "db-blob-";

        var sp = TestHost.BuildBaseServices(services =>
        {
            services.AddAzureBlobDatabase(dbId, opt =>
            {
                opt.ConnectionString = conn!;
                opt.ShardDepth = 2;
                opt.ShardWidth = 2;
                opt.Diff = new JsonDiffOptions
                {
                    ArrayOrderKeys = new[] { "Id", "Name" },
                    NormalizeArraysBeforeDiff = true,
                    DiffArrays = true
                };
            });
        });

        return (sp, dbId);
    }

    [SkippableFact]
    public async Task Blob_CRUD_and_Concurrency()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Blob-CRUD");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.CrudAndConcurrencyAsync(manager, dbId, "People");
    }

    [SkippableFact]
    public async Task Blob_Table_Lifecycle()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Blob-Table");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.TableLifecycleAsync(manager, dbId, "Artifacts");
    }
}
