#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable
using Microsoft.Extensions.Logging;
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public static class StorageLogEvents
{
    public static readonly EventId OpStart   = new(1000, "StorageOperationStart");
    public static readonly EventId OpSuccess = new(1001, "StorageOperationSuccess");
    public static readonly EventId OpError   = new(1002, "StorageOperationError");
    public static readonly EventId Conflict  = new(1003, "StorageConflict");
    public static readonly EventId NotFound  = new(1004, "StorageNotFound");
}
