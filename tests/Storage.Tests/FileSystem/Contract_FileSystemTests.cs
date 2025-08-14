using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.IO;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Cloudbrick.DataExplorer.Storage.Tests.Shared;

namespace Cloudbrick.DataExplorer.Storage.Tests.FileSystem;

public class Contract_FileSystemTests
{
    private static (ServiceProvider sp, string dbId) Build()
    {
        var root = Path.Combine(Path.GetTempPath(), "rm-tests-fs");
        Directory.CreateDirectory(root);

        var sp = TestHost.BuildBaseServices(services =>
        {
            services.AddLocalFileSystemDatabase("db-fs", opt =>
            {
                opt.Root = root;
                opt.ShardDepth = 2;
                opt.ShardWidth = 2;
                opt.Diff = new JsonDiffOptions
                {
                    RootPath = "", // comparing the Data object directly
                    NormalizeArraysBeforeDiff = true,
                    DiffArrays = true,
                    ArrayOrderKeys = new[] { "Id", "Name", "Timestamp" }, // <-- important
                    MaxArrayItems = 10_000 // optional: lift cap if needed
                };
            });
        });

        return (sp, "db-fs");
    }

    [Fact]
    public async Task FileSystem_CRUD_and_Concurrency()
    {
        var (sp, dbId) = Build();
        using var _ = TestHost.PushContext(sp, "FS-CRUD");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.CrudAndConcurrencyAsync(manager, dbId, "People");
    }

    [Fact]
    public async Task FileSystem_Table_Lifecycle()
    {
        var (sp, dbId) = Build();
        using var _ = TestHost.PushContext(sp, "FS-Table");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.TableLifecycleAsync(manager, dbId, "Artifacts");
    }
}
