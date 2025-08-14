using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using Cloudbrick.DataExplorer.Storage.Provider.AzureTable;
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Cloudbrick.DataExplorer.Storage.Provider.Sql;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.Design;

namespace Cloudbrick.DataExplorer.Storage
{
    internal class ResourceManagerServiceContext : IResourceManagerServiceContext
    {
        StorageProviderKind providerRegistered = StorageProviderKind.None;

        private IServiceCollection _services;

        public ResourceManagerServiceContext(IServiceCollection services)
        {
            _services = services;
        }

        public void AddLocalFileSystemDatabase(string databaseId, Action<FileSystemOptions> fileSystemOptions)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.LocalFileSystem);
            _services.AddLocalFileSystemDatabase(databaseId, fileSystemOptions);
        }
        public void AddAzureBlobDatabase(string databaseId, Action<BlobOptions> fileSystemOptions)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.AzureBlobStorage);
            _services.AddAzureBlobDatabase(databaseId, fileSystemOptions);
        }
        public void AddLocalFileSystemDatabase(string databaseId, Action<TableOptions> fileSystemOptions)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.AzureTableStorage);
            _services.AddAzureTableDatabase(databaseId, fileSystemOptions);
        }
        public void AddCosmosDatabase(string databaseId, Action<CosmosOptions> fileSystemOptions)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.CosmosDb);
            _services.AddCosmosDatabase(databaseId, fileSystemOptions);
        }
        public void AddSqlDatabase(string databaseId, Action<SqlOptions> fileSystemOptions)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.SqlDatabase);
            _services.AddSqlDatabase(databaseId, fileSystemOptions);
        }

        private void ThrowIfProviderAlreadyRegistered(StorageProviderKind toRegister)
        {
            if (providerRegistered != StorageProviderKind.None)
            {
                throw new Exception($"Provider kind '{providerRegistered}' already registered.");
            }
            else
            {
                providerRegistered = toRegister;
            }
        }
    }
}
