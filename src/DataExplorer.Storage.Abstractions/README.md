# DataExplorer.Storage.Abstractions

## Purpose
Defines the core contracts for the storage system including database, table and provider abstractions.

## Public APIs
- `IStorageManager` and `IConfigAwareStorageManager` for high-level data access.
- `IDatabaseContext`, `ITableContext`, and `ITableQuery` for database and table operations.
- `IStorageProvider` and `IProviderFactory` for pluggable storage backends.
- Option and telemetry types such as `ProviderOptionsBase`, `StorageTelemetry`, and `RetryOptions`.

## Build
```bash
dotnet build src/DataExplorer.Storage.Abstractions/Cloudbrick.DataExplorer.Storage.Abstractions.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
