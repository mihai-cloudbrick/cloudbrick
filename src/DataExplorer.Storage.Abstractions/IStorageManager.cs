#nullable enable

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Resolves <see cref="IDatabaseContext"/> instances by database identifier.
/// </summary>
/// <remarks>
/// Implementations manage provider lifetimes and may cache contexts for efficiency.
/// </remarks>
public interface IStorageManager
{
    /// <summary>
    /// Gets a context for interacting with the specified database.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <returns>A database context.</returns>
    /// <remarks>
    /// Throws if the database cannot be resolved or accessed.
    /// </remarks>
    IDatabaseContext Database(string databaseId);
}
