#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.Orleans.Reminders.DataExplorer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Cloudbricks.Orleans.Tests;

public class ReminderTableTests
{
    [Fact]
    public async Task Reminders_Upsert_Read_Remove_Should_Work()
    {
        var (sp, root) = TestHost.Build();
        try
        {
            var mgr = sp.GetRequiredService<IConfigAwareStorageManager>();
            var logger = sp.GetRequiredService<ILogger<DataExplorerReminderTable>>();
            var options = Options.Create(new DataExplorerReminderOptions { DatabaseId = "orleans-db", TablePrefix = "Reminders", Buckets = 64 });
            var table = new DataExplorerReminderTable(mgr, options, logger);

            await table.Init("svc1", SiloAddress.New(new IPEndPoint(IPAddress.Any, 60000), 1));

            var grain = GrainId.Create("ns", "user-42");
            var entry = new ReminderEntry
            {
                StartAt = DateTime.UtcNow.AddMinutes(1),
                ETag = string.Empty,
                GrainId = grain,
                Period = TimeSpan.FromMinutes(5),
                ReminderName = "ping"
            };

            var etag = await table.UpsertRow(entry);
            etag.Should().NotBeNullOrEmpty();

            // Read by range (full space)
            var begin = 0u; var end = 0xffffffffu;
            var rows = await table.ReadRows(begin, end);
            rows.Reminders.Should().ContainSingle(r => r.GrainId == grain && r.ReminderName == "ping");

            // Remove
            var removed = await table.RemoveRow(grain, "ping", etag);
            removed.Should().BeTrue();

            // Verify gone
            rows = await table.ReadRows(begin, end);
            rows.Reminders.Should().BeEmpty();
        }
        finally
        {
            try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
        }
    }
}
