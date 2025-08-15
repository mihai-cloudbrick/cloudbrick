# DataExplorer.Storage.Configuration

## Purpose
Provides default implementations for configuring storage providers, managing database registrations, and supplying execution context and diff settings.

## Public APIs
- `ServiceCollectionExtensions.AddResourceManagerStorageCore` to register core services.
- `ConfiguredStorageManager` and `InMemoryDatabaseConfigManager` for configuration-based access.
- `IStorageProviderBuilder` implementations and helpers like `DefaultExecutionContextAccessor`.

## Build
```bash
dotnet build src/DataExplorer.Storage.Configuration/Cloudbrick.DataExplorer.Storage.Configuration.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
