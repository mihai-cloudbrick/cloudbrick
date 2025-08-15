#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IProviderFactory
{
    IStorageProvider Create(string databaseId, IProviderOptions options);
}
