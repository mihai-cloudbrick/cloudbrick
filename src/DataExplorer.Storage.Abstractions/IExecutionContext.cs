#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Describes contextual information for an execution flow.
/// </summary>
/// <remarks>
/// Used for logging and telemetry to correlate operations.
/// </remarks>
public interface IExecutionContext
{
    /// <summary>
    /// Gets the name of the action being executed.
    /// </summary>
    string ActionName { get; }

    /// <summary>
    /// Gets a tracking identifier for correlation.
    /// </summary>
    string TrackingId { get; }

    /// <summary>
    /// Gets the identifier of the acting principal.
    /// </summary>
    string PrincipalId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the action began.
    /// </summary>
    DateTimeOffset StartedAtUtc { get; }
}
