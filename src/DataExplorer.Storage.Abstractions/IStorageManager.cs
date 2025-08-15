#nullable enable

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IStorageManager
{
    IDatabaseContext Database(string databaseId);
}
