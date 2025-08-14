#nullable enable
using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Provider.AzureTable;
using Cloudbrick.DataExplorer.Storage.Tests;
using Cloudbrick.DataExplorer.Storage.Tests.Shared;

namespace Cloudbrick.DataExplorer.Storage.Tests.AzureTable;

public class Contract_AzureTableTests
{
    private static (ServiceProvider sp, string dbId) BuildOrSkip()
    {
        var conn = Constants.AzureStorageConnectionString;

        Skip.If(string.IsNullOrWhiteSpace(conn), "Set TEST_TABLE_CONN to run Azure Table tests (Azurite or real).");

        var dbId = "db-table";

        var sp = TestHost.BuildBaseServices(services =>
        {
            services.AddAzureTableDatabase(dbId, opt =>
            {
                opt.ConnectionString = conn!;
                opt.UseDatabasePrefix = true;
                opt.Separator = "_";
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
    public async Task Table_CRUD_and_Concurrency()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Table-CRUD");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.CrudAndConcurrencyAsync(manager, dbId, "People");
    }

    [SkippableFact]
    public async Task Table_Table_Lifecycle()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Table-Table");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.TableLifecycleAsync(manager, dbId, "Artifacts");
    }
}
