#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Provides query capabilities for a table.
/// </summary>
/// <remarks>
/// Implementations may support server-side filtering and pagination.
/// </remarks>
public interface ITableQuery
{
    /// <summary>
    /// Executes a query and returns a single result set.
    /// </summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The result of the query.</returns>
    /// <remarks>
    /// Throws if the query fails or <paramref name="spec"/> is invalid.
    /// </remarks>
    Task<StorageQueryResult> QueryAsync(QuerySpec spec, CancellationToken ct = default);

    /// <summary>
    /// Executes a query and yields results page by page.
    /// </summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>An asynchronous sequence of query results.</returns>
    /// <remarks>
    /// Throws if the query fails or <paramref name="spec"/> is invalid.
    /// </remarks>
    IAsyncEnumerable<StorageQueryResult> QueryPagesAsync(QuerySpec spec, CancellationToken ct = default);
}
