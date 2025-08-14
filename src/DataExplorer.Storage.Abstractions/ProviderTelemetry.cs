#nullable enable
using System.Diagnostics;
using Microsoft.Extensions.Logging;
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public static class ProviderTelemetry
{
    public static async Task<T> WithTelemetryAsync<T>(
        ILogger logger,
        string operation,
        string dbSystem,
        string databaseId,
        string? tableId,
        RetryOptions? retryOptions,
        Func<CancellationToken, Task<T>> action,
        Func<T, (string Status, long Rows)> classify,
        CancellationToken ct)
    {
        using var activity = StorageTelemetry.Source.StartActivity($"storage.{operation}", ActivityKind.Internal);
        activity?.SetTag("db.system", dbSystem);
        activity?.SetTag("db.name", databaseId);
        if (!string.IsNullOrWhiteSpace(tableId)) activity?.SetTag("table", tableId);
        activity?.SetTag("rm.operation", operation);
        logger.LogOpStart(operation, databaseId, tableId);
        var sw = Stopwatch.StartNew();
        try
        {
            var result = await RetryPolicyFactory.ExecuteAsync(action, retryOptions, ct).ConfigureAwait(false);
            sw.Stop();
            var (status, rows) = classify(result);
            activity?.SetTag("status", status);
            activity?.SetTag("rows", rows);
            StorageMetrics.Record(operation, databaseId, tableId, status, rows, sw.Elapsed.TotalMilliseconds);
            logger.LogOpSuccess(operation, databaseId, tableId, status, (int)rows, sw.Elapsed.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            StorageMetrics.Record(operation, databaseId, tableId, "ERROR", 0, sw.Elapsed.TotalMilliseconds);
            logger.LogOpError(operation, databaseId, tableId, ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
