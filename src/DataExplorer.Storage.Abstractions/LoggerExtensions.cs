#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Microsoft.Extensions.Logging;
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public static class LoggerExtensions
{
    public static IDisposable BeginExecutionScope(this ILogger logger, string actionName, string trackingId, string principalId, string databaseId, string? tableId, string? operation)
        => logger.BeginScope(new Dictionary<string, object?>
        {
            ["ActionName"] = actionName,
            ["TrackingId"] = trackingId,
            ["PrincipalId"] = principalId,
            ["DatabaseId"] = databaseId,
            ["TableId"] = tableId,
            ["Operation"] = operation
        });
    public static void LogOpStart(this ILogger logger, string op, string db, string? table)
        => logger.LogInformation(StorageLogEvents.OpStart, "Start {Operation} db={DatabaseId} table={TableId}", op, db, table);
    public static void LogOpSuccess(this ILogger logger, string op, string db, string? table, string status, int rows, double ms)
        => logger.LogInformation(StorageLogEvents.OpSuccess, "Done {Operation} db={DatabaseId} table={TableId} status={Status} rows={Rows} in {ElapsedMs}ms", op, db, table, status, rows, ms);
    public static void LogOpError(this ILogger logger, string op, string db, string? table, Exception ex)
        => logger.LogError(StorageLogEvents.OpError, ex, "Error {Operation} db={DatabaseId} table={TableId}", op, db, table);
}
