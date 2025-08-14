using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Provider.Sql;
using Cloudbrick.DataExplorer.Storage.Tests;
using Cloudbrick.DataExplorer.Storage.Tests.Shared;

namespace Cloudbrick.DataExplorer.Storage.Tests.Sql;

public class Contract_SqlTests
{
    private static (ServiceProvider sp, string dbId) BuildOrSkip()
    {
        var conn = Constants.SqlConnectionString;
        Skip.If(string.IsNullOrWhiteSpace(conn),
            "Set TEST_SQL_CONN to a valid SQL Server connection string (DB must exist) to run SQL tests.");

        var dbId = "db-sql-";

        var sp = TestHost.BuildBaseServices(services =>
        {
            services.AddSqlDatabase(dbId, opt =>
            {
                opt.ConnectionString = conn!;
                opt.Schema = "dbo";
                opt.Diff = new JsonDiffOptions
                {
                    ArrayOrderKeys = new[] { "Id" },
                    NormalizeArraysBeforeDiff = true,
                    DiffArrays = true
                };
            });
        });

        return (sp, dbId);
    }

    [SkippableFact]
    public async Task Sql_CRUD_and_Concurrency()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Sql-CRUD");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.CrudAndConcurrencyAsync(manager, dbId, "People");
    }

    [SkippableFact]
    public async Task Sql_Table_Lifecycle()
    {
        var (sp, dbId) = BuildOrSkip();
        using var _ = TestHost.PushContext(sp, "Sql-Table");
        var manager = await TestHost.BuildManager(sp);

        await ContractTests.TableLifecycleAsync(manager, dbId, "Artifacts");
    }
}
