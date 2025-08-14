#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Diagnostics.Metrics;
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public static class StorageMetrics
{
    public const string MeterName = "Cloudbrick.DataExplorer.Storage";
    public static readonly Meter Meter = new(MeterName);
    public static readonly Counter<long> OpsCounter =
        Meter.CreateCounter<long>("storage.ops", "ops", "Storage operations count");
    public static readonly Histogram<double> OpLatencyMs =
        Meter.CreateHistogram<double>("storage.op.duration.ms", "ms", "Operation latency (ms)");
    public static void Record(string operation, string db, string? table, string status, long rows = 0, double? ms = null)
    {
        OpsCounter.Add(1, new("operation", operation), new("db", db), new("table", table ?? ""), new("status", status));
        if (ms is double v) OpLatencyMs.Record(v, new("operation", operation), new("db", db), new("table", table ?? ""), new("status", status), new("rows", rows));
    }
}
