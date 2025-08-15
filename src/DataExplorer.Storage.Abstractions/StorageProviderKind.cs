namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public enum StorageProviderKind
{
    LocalFileSystem,
    AzureBlobStorage,
    AzureTableStorage,
    AzureCosmosDb,
    SqlDatabase,
    None
}
