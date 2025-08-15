#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Provides access to the current <see cref="IExecutionContext"/>.
/// </summary>
/// <remarks>
/// Implementations may store context in asynchronous local state. When no context is available, <c>null</c> is returned.
/// </remarks>
public interface IExecutionContextAccessor
{
    /// <summary>
    /// Gets the current execution context if one has been established.
    /// </summary>
    /// <remarks>
    /// Returns <c>null</c> when no execution context is present.
    /// </remarks>
    IExecutionContext? Current { get; }
}
