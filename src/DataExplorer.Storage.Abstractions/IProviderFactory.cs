#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Creates <see cref="IStorageProvider"/> instances based on configuration.
/// </summary>
/// <remarks>
/// Implementations select the appropriate provider implementation for a database and options combination.
/// </remarks>
public interface IProviderFactory
{
    /// <summary>
    /// Creates a storage provider for the specified database and options.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <param name="options">Provider configuration options.</param>
    /// <returns>An initialized storage provider.</returns>
    /// <remarks>
    /// Throws if the provider cannot be created or the options are invalid.
    /// </remarks>
    IStorageProvider Create(string databaseId, IProviderOptions options);
}
