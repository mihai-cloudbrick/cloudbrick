#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Creates execution scopes used for logging and telemetry.
/// </summary>
/// <remarks>
/// Scopes tie an <see cref="IExecutionContext"/> to log entries. Implementations should clean up resources when the scope is disposed.
/// </remarks>
public interface IExecutionScopeFactory
{
    /// <summary>
    /// Begins a new execution scope.
    /// </summary>
    /// <param name="logger">The logger used to create the scope.</param>
    /// <param name="ctx">The execution context for the scope.</param>
    /// <returns>A disposable that ends the scope when disposed.</returns>
    /// <remarks>
    /// Throws if <paramref name="logger"/> or <paramref name="ctx"/> is <c>null</c>.
    /// </remarks>
    IDisposable Begin(ILogger logger, IExecutionContext ctx);
}
