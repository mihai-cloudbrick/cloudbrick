#nullable enable

using Cloudbrick.DataExplorer.Storage.Configuration;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// A simple execution context implementation you can use directly in apps/tests.
/// </summary>
public sealed class ExecutionContext : IExecutionContext
{
    public string ActionName { get; set; } = "Unknown";
    public string TrackingId { get; set; } = Guid.NewGuid().ToString("N");
    public string PrincipalId { get; set; } = "anonymous";
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
