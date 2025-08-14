#nullable enable
using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Cloudbrick.DataExplorer.Storage.Tests;
using Cloudbrick.DataExplorer.Storage.Tests.Shared;

namespace Cloudbrick.DataExplorer.Storage.Tests.Cosmos;

public class Contract_CosmosTests
{
    private static (ServiceProvider sp, string dbId) BuildOrSkip()
    {
        var endpoint = Constants.CosmosAccountEndpoint;
        var key = Constants.CosmosAccountKey;
        Skip.If(string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key),
            "Set TEST_COSMOS_ENDPOINT and TEST_COSMOS_KEY to run Cosmos tests (Emulator or real).");

        var dbId = "db-cosmos";

        var sp = TestHost.BuildBaseServices(services =>
        {
            services.AddAzureCosmosDatabase(dbId, opt =>
            {
                opt.Endpoint = endpoint!;
                opt.Key = key!;
                opt.PartitionKeyPath = "/id";
                opt.DefaultThroughput = 400;
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
    public async Task Cosmos_CRUD_and_Concurrency()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Cosmos-CRUD");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.CrudAndConcurrencyAsync(manager, dbId, "People");
    }

    [SkippableFact]
    public async Task Cosmos_Table_Lifecycle()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Cosmos-Table");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.TableLifecycleAsync(manager, dbId, "Artifacts");
    }
}
