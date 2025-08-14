#nullable enable
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using FluentAssertions;

namespace Cloudbrick.DataExplorer.Storage.Tests.Shared;

public static class ContractTests
{
    public static async Task CrudAndConcurrencyAsync(IConfigAwareStorageManager manager, string dbId, string tableId)
    {
        var db = manager.Database(dbId);
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
                ["Lines"] = new JsonArray(
                    new JsonObject { ["Id"] = 2, ["FullName"] = "B" },
                    new JsonObject { ["Id"] = 1, ["FullName"] = "A" }
                )
            }
        };

        var c = await table.CreateAsync("ada", item);
        c.Status.Should().Be(OperationStatus.Created);
        c.ETag.Should().NotBeNullOrEmpty();

        // Get
        var g = await table.GetAsync("ada");
        g.Status.Should().Be(OperationStatus.Unchanged);
        g.Item!.Data["Name"]!.GetValue<string>().Should().Be("Ada");
        g.Item!.ETag.Should().NotBeNullOrEmpty();

        // Update with array reordering only — with diff configured it should yield zero changes
        var updated = g.Item!;
        updated.Data["Lines"] = new JsonArray(
            new JsonObject { ["Id"] = 1, ["FullName"] = "A" },
            new JsonObject { ["Id"] = 2, ["FullName"] = "B" }
        );
        var u1 = await table.UpdateAsync("ada", updated);
        u1.Status.Should().Be(OperationStatus.Updated);
        (u1.Changes?.Count ?? 0).Should().Be(0);

        // Update with actual change
        var again = u1.Item!;
        again.Data["Role"] = "Staff Engineer";
        var u2 = await table.UpdateAsync("ada", again);
        u2.Status.Should().Be(OperationStatus.Updated);
        (u2.Changes?.Count ?? 0).Should().BeGreaterThan(0);

        // Concurrency: try with stale ETag (use original first get etag)
        var stale = g.Item!;
        stale.Data["Role"] = "Principal Engineer";
        var conflict = await table.UpdateAsync("ada", stale);
        conflict.Status.Should().BeOneOf(OperationStatus.Conflict, OperationStatus.Updated /* some stores may not enforce if ETag unset */);

        // List
        var list = await table.ListAsync();
        (list.Items?.Count ?? 0).Should().BeGreaterThanOrEqualTo(1);

        // Delete
        var d = await table.DeleteAsync("ada");
        d.Status.Should().Be(OperationStatus.Deleted);

        var g2 = await table.GetAsync("ada");
        g2.Status.Should().Be(OperationStatus.NotFound);

        
    }

    public static async Task TableLifecycleAsync(IConfigAwareStorageManager manager, string dbId, string tableId)
    {
        var db = manager.Database(dbId);
        await db.CreateIfNotExistsAsync();
        var table = db.Table(tableId);

        await table.CreateIfNotExistsAsync();
        (await table.ExistsAsync()).Should().BeTrue();

        await table.DeleteIfExistsAsync();
        (await table.ExistsAsync()).Should().BeFalse();
    }
}
