#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Represents an operational context for a database.
/// </summary>
/// <remarks>
/// Exposes administrative operations and access to table contexts.
/// </remarks>
public interface IDatabaseContext
{
    // Admin

    /// <summary>
    /// Creates the database if it does not exist.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Throws if the database cannot be created.
    /// </remarks>
    Task CreateIfNotExistsAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes the database if it exists.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Throws if deletion fails.
    /// </remarks>
    Task DeleteIfExistsAsync(CancellationToken ct = default);

    /// <summary>
    /// Lists the tables contained in the database.
    /// </summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A read-only list of table information.</returns>
    /// <remarks>
    /// Throws if retrieval fails.
    /// </remarks>
    Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default);

    // Data

    /// <summary>
    /// Gets a context for the specified table.
    /// </summary>
    /// <param name="tableId">The table identifier.</param>
    /// <returns>A table context for performing operations.</returns>
    /// <remarks>
    /// Throws if the table does not exist or cannot be accessed.
    /// </remarks>
    ITableContext Table(string tableId);

}
