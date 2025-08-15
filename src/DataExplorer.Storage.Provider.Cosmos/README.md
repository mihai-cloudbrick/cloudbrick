# DataExplorer.Storage.Provider.Cosmos

## Purpose
Adds a provider implementation backed by Azure Cosmos DB.

## Public APIs
- `CosmosStorageProvider` and `CosmosDatabaseContext` for Cosmos containers.
- `CosmosOptions` to configure endpoint, key, and partitioning.
- `CosmosProviderBuilder` and `RegistrationExtensions.AddAzureCosmosDatabase` for dependency injection.

## Build
```bash
dotnet build src/DataExplorer.Storage.Provider.Cosmos/Cloudbrick.DataExplorer.Storage.Provider.Cosmos.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
