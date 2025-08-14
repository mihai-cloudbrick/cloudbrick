#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IExecutionContext
{
    string ActionName { get; }
    string TrackingId { get; }
    string PrincipalId { get; }
    DateTimeOffset StartedAtUtc { get; }
}
