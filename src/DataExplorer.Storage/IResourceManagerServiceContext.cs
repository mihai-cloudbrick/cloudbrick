using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using Cloudbrick.DataExplorer.Storage.Provider.AzureTable;
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Cloudbrick.DataExplorer.Storage.Provider.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.DataExplorer.Storage
{
    public interface IResourceManagerServiceContext
    {
        void AddAzureBlobDatabase(string databaseId, Action<BlobOptions> fileSystemOptions);
        void AddCosmosDatabase(string databaseId, Action<CosmosOptions> fileSystemOptions);
        void AddLocalFileSystemDatabase(string databaseId, Action<FileSystemOptions> fileSystemOptions);
        void AddLocalFileSystemDatabase(string databaseId, Action<TableOptions> fileSystemOptions);
        void AddSqlDatabase(string databaseId, Action<SqlOptions> fileSystemOptions);
    }
}