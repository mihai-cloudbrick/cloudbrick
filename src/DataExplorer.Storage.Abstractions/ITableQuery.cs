#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface ITableQuery
{
    Task<StorageQueryResult> QueryAsync(QuerySpec spec, CancellationToken ct = default);
    IAsyncEnumerable<StorageQueryResult> QueryPagesAsync(QuerySpec spec, CancellationToken ct = default);
}
