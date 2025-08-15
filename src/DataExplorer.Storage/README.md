# DataExplorer.Storage

## Purpose
Provides dependency injection extensions to configure DataExplorer storage and register a single backing provider.

## Public APIs
- `IResourceManagerServiceContext` for registering providers.
- `ServiceCollectionExtensions.AddResourceManagerStorage` to add core services and obtain a configuration context.

## Build
```bash
dotnet build src/DataExplorer.Storage/Cloudbrick.DataExplorer.Storage.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
