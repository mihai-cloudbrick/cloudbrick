#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;

#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Creates a structured logging scope with Action, TrackingId, PrincipalId.
/// </summary>
public sealed class DefaultExecutionScopeFactory : IExecutionScopeFactory
{
    private static readonly IReadOnlyDictionary<string, object?> _empty = new Dictionary<string, object?>();

    public IDisposable Begin(ILogger logger, IExecutionContext ctx)
    {
        var state = new Dictionary<string, object?>
        {
            ["Action"] = ctx.ActionName,
            ["TrackingId"] = ctx.TrackingId,
            ["PrincipalId"] = ctx.PrincipalId,
            ["StartedAtUtc"] = ctx.StartedAtUtc
        };

        return logger.BeginScope(state);
    }
}
