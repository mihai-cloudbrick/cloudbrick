#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Storage manager that is aware of database configuration and provider factories.
/// </summary>
/// <remarks>
/// Extends <see cref="IStorageManager"/> by exposing configuration services and initialization routines.
/// </remarks>
public interface IConfigAwareStorageManager : IStorageManager
{
    /// <summary>
    /// Gets the configuration manager for databases.
    /// </summary>
    IDatabaseConfigManager Config { get; }

    /// <summary>
    /// Gets the factory responsible for creating storage providers.
    /// </summary>
    IProviderFactory Providers { get; }

    /// <summary>
    /// Initializes the storage manager.
    /// </summary>
    /// <param name="createStructuresIfMissing">Indicates whether missing structures should be created.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Throws if initialization fails.
    /// </remarks>
    Task InitializeAsync(bool createStructuresIfMissing = true, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached provider for a database.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <remarks>
    /// Subsequent calls to <see cref="IStorageManager.Database"/> will create a new provider for the database.
    /// </remarks>
    void Invalidate(string databaseId);

    /// <summary>
    /// Invalidates all cached providers.
    /// </summary>
    /// <remarks>
    /// Useful when global configuration changes occur.
    /// </remarks>
    void InvalidateAll();
}
