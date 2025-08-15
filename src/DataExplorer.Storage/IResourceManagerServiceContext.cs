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
        void AddAzureBlobsDatabase(string databaseId, Action<BlobOptions> options);
        void AddAzureCosmosDatabase(string databaseId, Action<CosmosOptions> options);
        void AddLocalFileSystemDatabase(string databaseId, Action<FileSystemOptions> options);
        void AddAzureTablesDatabase(string databaseId, Action<TableOptions> options);
        void AddSqlDatabase(string databaseId, Action<SqlOptions> fileSystemOptions);
    }
}