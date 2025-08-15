using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using Cloudbrick.DataExplorer.Storage.Provider.AzureTable;
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using Cloudbrick.DataExplorer.Storage.Provider.Sql;
using Microsoft.Extensions.DependencyInjection;

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

        public void AddLocalFileSystemDatabase(string databaseId, Action<FileSystemOptions> options)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.LocalFileSystem);
            _services.AddLocalFileSystemDatabase(databaseId, options);
        }
        public void AddAzureBlobsDatabase(string databaseId, Action<BlobOptions> options)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.AzureBlobStorage);
            _services.AddAzureBlobDatabase(databaseId, options);
        }
        public void AddAzureTablesDatabase(string databaseId, Action<TableOptions> options)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.AzureTableStorage);
            _services.AddAzureTableDatabase(databaseId, options);
        }
        public void AddAzureCosmosDatabase(string databaseId, Action<CosmosOptions> options)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.AzureCosmosDb);
            _services.AddAzureCosmosDatabase(databaseId, options);
        }
        public void AddSqlDatabase(string databaseId, Action<SqlOptions> options)
        {
            ThrowIfProviderAlreadyRegistered(StorageProviderKind.SqlDatabase);
            _services.AddSqlDatabase(databaseId, options);
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
