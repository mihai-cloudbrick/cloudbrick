# Cloudbrick (Out-of-the-box)

This solution builds and runs **without external NuGet dependencies** beyond .NET 8.

## Components
- `Cloudbrick.Components.Blades` — Azure-Portal style blade controls for Blazor.
- `Cloudbrick.Components.Jobs` — Fluent UI components for viewing and controlling jobs with live telemetry.

## DataExplorer storage
- `Cloudbrick.DataExplorer.Storage.Abstractions` — core types, telemetry scaffold, retry, JSON diff.
- `Cloudbrick.DataExplorer.Storage.Configuration` — DB registration, provider factory, config-aware manager, DI.
- `Cloudbrick.DataExplorer.Storage` — service context helpers.
- Providers:
  - `Cloudbrick.DataExplorer.Storage.Provider.FileSystem` — local FileSystem provider with sharded SHA-256 file names, ETag, optional AES-GCM encryption.
  - `Cloudbrick.DataExplorer.Storage.Provider.Sql` — SQL-based provider.
  - `Cloudbrick.DataExplorer.Storage.Provider.AzureBlob` — Azure Blob provider.
  - `Cloudbrick.DataExplorer.Storage.Provider.AzureTable` — Azure Table provider.
  - `Cloudbrick.DataExplorer.Storage.Provider.Cosmos` — Cosmos DB provider.
- `Cloudbrick.DataExplorer.Storage.SampleApp` — console app showing end-to-end usage.

## Orleans
- `Cloudbrick.Orleans.Abstractions` — shared contracts for Orleans integration.
- `Cloudbrick.Orleans` — Orleans hosting helpers.
- `Cloudbrick.Orleans.Persistance.DataExplorer` — DataExplorer-backed storage for Orleans grains.
- `Cloudbrick.Orleans.Reminders.DataExplorer` — DataExplorer-backed reminder provider.
- `Cloudbrick.Orleans.SignalR` — SignalR integration.
- `Cloudbrick.Orleans.Jobs.Abstractions` and `Cloudbrick.Orleans.Jobs` — job grain contracts and implementations.

## Run
```bash
dotnet build Cloudbrick.sln
dotnet run --project samples/Storage.SampleApp/Cloudbrick.DataExplorer.Storage.SampleApp.csproj
```
This will create a `data/` folder under the app base directory and demonstrate create/get/update/list/query/delete.

## Enable encryption-at-rest
Update the registration:
```csharp
Encryption = new EncryptionOptions { Enabled = true, KeyBase64 = "<base64-32-byte-key>", KeyId = "k1" }
```
Use `openssl rand -base64 32` to generate a key.

## Notes
- Additional providers (SQL, Azure Blob/Table, Cosmos) can be added by implementing `IDatabaseContext` and plugging into `StorageProviderFactory`.
- The Kusto loader currently scans via `ListAsync`. Plug your Kusto-Loco engine where indicated to evaluate KQL.
