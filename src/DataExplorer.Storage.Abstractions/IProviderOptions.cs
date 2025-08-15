#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable


namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Defines configuration options for a storage provider.
/// </summary>
/// <remarks>
/// Implementations expose provider-specific settings. Invalid values may lead to runtime failures when a provider is created.
/// </remarks>
public interface IProviderOptions
{
    /// <summary>
    /// Gets the kind of storage provider that these options target.
    /// </summary>
    /// <remarks>
    /// Implementations typically use this value to resolve the provider implementation. An unsupported value may result in an exception.
    /// </remarks>
    StorageProviderKind Kind { get; }

    /// <summary>
    /// Optional per-database diff settings; falls back to <see cref="JsonDiffOptions.Default"/> if <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Returned options influence how document differences are calculated.
    /// </remarks>
    JsonDiffOptions? Diff { get; }
}
