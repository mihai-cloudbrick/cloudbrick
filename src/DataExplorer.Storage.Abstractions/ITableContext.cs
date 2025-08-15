#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Provides CRUD and administrative operations for a storage table.
/// </summary>
/// <remarks>
/// Implementations should honour cancellation requests and surface provider-specific failures via exceptions or <see cref="StorageResult{T}"/>.
/// </remarks>
public interface ITableContext
{
    // Admin

    /// <summary>
    /// Creates the table if it does not already exist.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Throws if the table cannot be created.
    /// </remarks>
    Task CreateIfNotExistsAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes the table if it exists.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Throws if the deletion fails.
    /// </remarks>
    Task DeleteIfExistsAsync(CancellationToken ct = default);

    /// <summary>
    /// Determines whether the table exists.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns><c>true</c> if the table exists; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Throws if the existence check fails.
    /// </remarks>
    Task<bool> ExistsAsync(CancellationToken ct = default);

    // CRUD

    /// <summary>
    /// Retrieves an item by identifier.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A storage result containing the item if found.</returns>
    /// <remarks>
    /// Returns a result indicating not found when the item does not exist; throws if retrieval fails.
    /// </remarks>
    Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Lists items in the table with optional paging.
    /// </summary>
    /// <param name="skip">The number of items to skip.</param>
    /// <param name="take">The maximum number of items to return.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A storage result containing the retrieved items.</returns>
    /// <remarks>
    /// Throws if the list operation fails.
    /// </remarks>
    Task<StorageResult<StorageItem>> ListAsync(int? skip = null, int? take = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a new item in the table.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="entity">The item to create.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A storage result describing the created item.</returns>
    /// <remarks>
    /// Throws if an item with the same identifier already exists or creation fails.
    /// </remarks>
    Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing item in the table.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="entity">The updated item.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A storage result describing the updated item.</returns>
    /// <remarks>
    /// Throws if the item does not exist or the update fails.
    /// </remarks>
    Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an item from the table.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A storage result describing the deletion.</returns>
    /// <remarks>
    /// Returns a not found result when the item does not exist; throws if deletion fails.
    /// </remarks>
    Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default);

    // Capabilities (query etc.)

    /// <summary>
    /// Gets the capabilities supported by the table.
    /// </summary>
    /// <returns>A value describing supported features such as querying.</returns>
    /// <remarks>
    /// Use the returned capabilities to determine which operations are available.
    /// </remarks>
    TableCapabilities GetCapabilities();
}
