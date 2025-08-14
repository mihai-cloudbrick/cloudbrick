#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IStorageProvider
{
    IDatabaseContext GetDatabase(string databaseId);
}
