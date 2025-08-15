# DataExplorer.Storage.Provider.AzureTable

## Purpose
Provides an Azure Table Storage backed implementation of the storage provider interfaces.

## Public APIs
- `TableStorageProvider` and `TableDatabaseContext` for table storage access.
- `TableOptions` to set connection details and naming behavior.
- `AzureTableProviderBuilder` and `RegistrationExtensions.AddAzureTableDatabase` for DI wiring.

## Build
```bash
dotnet build src/DataExplorer.Storage.Provider.AzureTable/Cloudbrick.DataExplorer.Storage.Provider.AzureTable.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
