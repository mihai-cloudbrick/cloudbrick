#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IConfigAwareStorageManager : IStorageManager
{
    IDatabaseConfigManager Config { get; }
    IProviderFactory Providers { get; }

    Task InitializeAsync(bool createStructuresIfMissing = true, CancellationToken ct = default);
    void Invalidate(string databaseId);
    void InvalidateAll();
}
