# Orleans.Persistance.DataExplorer

## Purpose
Implements an Orleans grain storage provider using the DataExplorer storage abstractions.

## Public APIs
- `DataExplorerGrainStorage` implementing Orleans `IGrainStorage`.
- `DataExplorerOrleansStorageOptions` for configuring database and table names.
- `SiloBuilderExtensions.AddDataExplorerGrainStorage` to register the provider with a silo.

## Build
```bash
dotnet build src/Orleans.Persistance.DataExplorer/Cloudbrick.Orleans.Persistance.DataExplorer.csproj
```

## Samples
No dedicated sample is provided.
