#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IDatabaseContext
{
    // Admin
    Task CreateIfNotExistsAsync(CancellationToken ct = default);
    Task DeleteIfExistsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default);

    // Data
    ITableContext Table(string tableId);
    
}
