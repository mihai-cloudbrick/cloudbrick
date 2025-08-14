using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;
using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using Cloudbrick.DataExplorer.Storage.Provider.AzureTable;
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Cloudbrick.DataExplorer.Storage.Provider.Sql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using ExecutionContext = Cloudbrick.DataExplorer.Storage.Configuration.ExecutionContext;

static void PrintHeader(string title)
{
    Console.WriteLine();
    Console.WriteLine(new string('═', 80));
    Console.WriteLine($"  {title}");
    Console.WriteLine(new string('─', 80));
}

static void PrintResult<T>(string label, StorageResult<T> res)
{
    Console.WriteLine($"{label}: status={res.Status} duration={res.Duration.TotalMilliseconds:N0}ms etag={res.ETag ?? "-"}");
    if (res.Error != null) Console.WriteLine($"  error: {res.Error.Message}");
    if (res.Changes is { Count: > 0 })
    {
        Console.WriteLine("  changes:");
        foreach (var kv in res.Changes)
        {
            var oldStr = kv.Value.OldValue?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";
            var newStr = kv.Value.NewValue?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "null";
            Console.WriteLine($"    {kv.Key}: {oldStr} -> {newStr}  (by {kv.Value.PrincipalId} @ {kv.Value.TimestampUtc:O})");
        }
    }
    if (res.Logs is { Count: > 0 })
    {
        Console.WriteLine("  logs:");
        foreach (var log in res.Logs)
        {
            var props = (log.Properties is { Count: > 0 })
                ? " " + string.Join(", ", log.Properties!.Select(p => $"{p.Key}={p.Value}"))
                : "";
            Console.WriteLine($"    [{log.Level}] {log.Message}{props}");
        }
    }
}

// DI
var services = new ServiceCollection();
services.AddLogging(b => b.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
}).SetMinimumLevel(LogLevel.Information));
services.AddResourceManagerStorageCore();

// Register provider builders
services.AddSingleton<IStorageProviderBuilder, FileSystemProviderBuilder>();
services.AddSingleton<IStorageProviderBuilder, SqlProviderBuilder>();
services.AddSingleton<IStorageProviderBuilder, AzureBlobProviderBuilder>();
services.AddSingleton<IStorageProviderBuilder, AzureTableProviderBuilder>();
services.AddSingleton<IStorageProviderBuilder, CosmosProviderBuilder>();

var sp = services.BuildServiceProvider();

// ---- REGISTER DATABASES IN CONFIG MANAGER (typed options) ----
var cfg = sp.GetRequiredService<IDatabaseConfigManager>();

// 1) Local FileSystem (always)
var fsRoot = Path.Combine(Path.GetTempPath(), "rm-sample-fs");
Directory.CreateDirectory(fsRoot);
await cfg.AddOrUpdateAsync(new DatabaseRegistration(
    "db-fs",
    new FileSystemOptions
    {
        Root = fsRoot,
        ShardDepth = 2,
        ShardWidth = 2,
        Diff = new JsonDiffOptions
        {
            ArrayOrderKeys = new[] { "Id", "Name", "Timestamp" },
            NormalizeArraysBeforeDiff = true,
            DiffArrays = true
        }
    }));

// 2) Optional SQL
var sqlConn = Environment.GetEnvironmentVariable("TEST_SQL_CONN");
if (!string.IsNullOrWhiteSpace(sqlConn))
{
    await cfg.AddOrUpdateAsync(new DatabaseRegistration(
        "db-sql",
        new SqlOptions
        {
            ConnectionString = sqlConn!,
            Schema = "dbo",
            Diff = new JsonDiffOptions { ArrayOrderKeys = new[] { "Id" }, DiffArrays = true }
        }));
}

// 3) Optional Azure Blob
var blobConn = Environment.GetEnvironmentVariable("TEST_BLOB_CONN");
if (!string.IsNullOrWhiteSpace(blobConn))
{
    await cfg.AddOrUpdateAsync(new DatabaseRegistration(
        "db-blob",
        new BlobOptions
        {
            ConnectionString = blobConn!,
            ShardDepth = 2,
            ShardWidth = 2,
            Diff = new JsonDiffOptions { ArrayOrderKeys = new[] { "Id", "Name" }, DiffArrays = true }
        }));
}

// 4) Optional Azure Table
var tableConn = Environment.GetEnvironmentVariable("TEST_TABLE_CONN");
if (!string.IsNullOrWhiteSpace(tableConn))
{
    await cfg.AddOrUpdateAsync(new DatabaseRegistration(
        "db-table",
        new TableOptions
        {
            ConnectionString = tableConn!,
            UseDatabasePrefix = true,
            Separator = "_",
            Diff = new JsonDiffOptions { ArrayOrderKeys = new[] { "Id", "Name" }, DiffArrays = true }
        }));
}

// 5) Optional Cosmos
var cosmosEndpoint = Environment.GetEnvironmentVariable("TEST_COSMOS_ENDPOINT");
var cosmosKey = Environment.GetEnvironmentVariable("TEST_COSMOS_KEY");
if (!string.IsNullOrWhiteSpace(cosmosEndpoint) && !string.IsNullOrWhiteSpace(cosmosKey))
{
    await cfg.AddOrUpdateAsync(new DatabaseRegistration(
        "db-cosmos",
        new CosmosOptions
        {
            Endpoint = cosmosEndpoint!,
            Key = cosmosKey!,
            PartitionKeyPath = "/id",
            DefaultThroughput = 400,
            Diff = new JsonDiffOptions { ArrayOrderKeys = new[] { "Id", "Name" }, DiffArrays = true }
        }));
}


// ---- Initialize (creates tables/containers if provider supports it) ----
var manager = sp.GetRequiredService<IConfigAwareStorageManager>();
await manager.InitializeAsync(createStructuresIfMissing: true);

// ---- Execution context (for logging scopes & auditing) ----
var accessor = sp.GetRequiredService<DefaultExecutionContextAccessor>();
using (accessor.Push(new ExecutionContext
{
    ActionName = "SampleRun",
    TrackingId = Guid.NewGuid().ToString("N"),
    PrincipalId = "sample-user"
}))
{
    await DemoAsync(manager, "db-fs", "People");

    if (!string.IsNullOrWhiteSpace(sqlConn)) await DemoAsync(manager, "db-sql", "People");
    if (!string.IsNullOrWhiteSpace(blobConn)) await DemoAsync(manager, "db-blob", "People");
    if (!string.IsNullOrWhiteSpace(tableConn)) await DemoAsync(manager, "db-table", "People");
    if (!string.IsNullOrWhiteSpace(cosmosEndpoint) && !string.IsNullOrWhiteSpace(cosmosKey))
        await DemoAsync(manager, "db-cosmos", "People");
}

Console.WriteLine();
Console.WriteLine("✓ Done");

// --- demo workflow ---
static async Task DemoAsync(IConfigAwareStorageManager manager, string dbId, string tableId)
{
    PrintHeader($"Database: {dbId}  Table: {tableId}");

    var db = manager.Database(dbId);
    await db.DeleteIfExistsAsync();
    await db.CreateIfNotExistsAsync();
    var table = db.Table(tableId);
    await table.CreateIfNotExistsAsync();

    // Create
    var item = new StorageItem
    {
        Data = new JsonObject
        {
            ["Name"] = "Ada",
            ["Role"] = "Engineer",
            ["Tags"] = new JsonArray("b", "a", "c"),
            ["Skills"] = new JsonArray(
                new JsonObject { ["Id"] = 2, ["Name"] = "B" },
                new JsonObject { ["Id"] = 1, ["Name"] = "A" }
            )
        }
    };
    var c = await table.CreateAsync("ada", item);
    PrintResult("Create", c);

    // Get
    var g = await table.GetAsync("ada");
    PrintResult("Get", g);

    // Update with array reorder only
    var re = g.Item!;
    re.Data["Skills"] = new JsonArray(
        new JsonObject { ["Id"] = 1, ["Name"] = "A" },
        new JsonObject { ["Id"] = 2, ["Name"] = "B" }
    );
    re.Data["Tags"] = new JsonArray("a", "b", "c");
    var u1 = await table.UpdateAsync("ada", re);
    PrintResult("Update (reorder only)", u1);

    // Update with a real change
    var re2 = u1.Item!;
    re2.Data["Role"] = "Staff Engineer";
    var u2 = await table.UpdateAsync("ada", re2);
    PrintResult("Update (real change)", u2);

    // List
    var l = await table.ListAsync(take: 10);
    Console.WriteLine($"List: {l.Status} count={(l.Items?.Count ?? 0)}");

    // Delete
    var d = await table.DeleteAsync("ada");
    PrintResult("Delete", d);

    // Confirm
    var g2 = await table.GetAsync("ada");
    PrintResult("Get (after delete)", g2);
}
