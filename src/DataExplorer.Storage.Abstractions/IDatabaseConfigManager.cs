#nullable enable
#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Manages registration information for databases.
/// </summary>
/// <remarks>
/// Implementations persist configuration and should validate inputs. Provider failures should result in exceptions.
/// </remarks>
public interface IDatabaseConfigManager
{
    /// <summary>
    /// Adds a new registration or updates an existing one.
    /// </summary>
    /// <param name="config">The registration to persist.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Throws if <paramref name="config"/> is invalid or the operation fails.
    /// </remarks>
    Task AddOrUpdateAsync(DatabaseRegistration config, CancellationToken ct = default);

    /// <summary>
    /// Removes a registration for the specified database.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns><c>true</c> if a registration was removed; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Throws if the remove operation fails.
    /// </remarks>
    Task<bool> RemoveAsync(string databaseId, CancellationToken ct = default);

    /// <summary>
    /// Gets the registration for the specified database, if any.
    /// </summary>
    /// <param name="databaseId">The database identifier.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The registration or <c>null</c> if none exists.</returns>
    /// <remarks>
    /// Throws if retrieval fails.
    /// </remarks>
    Task<DatabaseRegistration?> GetAsync(string databaseId, CancellationToken ct = default);

    /// <summary>
    /// Lists all database registrations.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A read-only list of registrations.</returns>
    /// <remarks>
    /// Throws if listing fails.
    /// </remarks>
    Task<IReadOnlyList<DatabaseRegistration>> ListAsync(CancellationToken ct = default);
}
