#nullable enable
#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IDatabaseConfigManager
{
    Task AddOrUpdateAsync(DatabaseRegistration config, CancellationToken ct = default);
    Task<bool> RemoveAsync(string databaseId, CancellationToken ct = default);
    Task<DatabaseRegistration?> GetAsync(string databaseId, CancellationToken ct = default);
    Task<IReadOnlyList<DatabaseRegistration>> ListAsync(CancellationToken ct = default);
}
