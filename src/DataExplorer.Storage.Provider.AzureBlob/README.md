# DataExplorer.Storage.Provider.AzureBlob

## Purpose
Implements a storage provider backed by Azure Blob Storage.

## Public APIs
- `BlobStorageProvider` and `BlobDatabaseContext` for blob-based tables.
- `BlobOptions` to configure connection and sharding.
- `AzureBlobProviderBuilder` and `RegistrationExtensions.AddAzureBlobDatabase` for DI registration.

## Build
```bash
dotnet build src/DataExplorer.Storage.Provider.AzureBlob/Cloudbrick.DataExplorer.Storage.Provider.AzureBlob.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
