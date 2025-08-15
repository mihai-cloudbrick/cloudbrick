#nullable enable

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Resolves <see cref="IDatabaseContext"/> instances for a storage provider.
/// </summary>
/// <remarks>
/// Providers encapsulate provider-specific logic. Implementations should validate database identifiers and surface provider errors.
/// </remarks>
public interface IStorageProvider
{
    /// <summary>
    /// Gets a database context for the specified identifier.
    /// </summary>
    /// <param name="databaseId">The unique database identifier.</param>
    /// <returns>A context used to interact with the database.</returns>
    /// <remarks>
    /// Throws if the database cannot be resolved or accessed.
    /// </remarks>
    IDatabaseContext GetDatabase(string databaseId);
}
