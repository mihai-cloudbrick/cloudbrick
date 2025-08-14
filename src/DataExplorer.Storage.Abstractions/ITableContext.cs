#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface ITableContext
{
    // Admin
    Task CreateIfNotExistsAsync(CancellationToken ct = default);
    Task DeleteIfExistsAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(CancellationToken ct = default);

    // CRUD
    Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default);
    Task<StorageResult<StorageItem>> ListAsync(int? skip = null, int? take = null, CancellationToken ct = default);
    Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default);
    Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default);
    Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default);

    // Capabilities (query etc.)
    TableCapabilities GetCapabilities();
}
