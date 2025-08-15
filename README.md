# ResourceManager.Storage (Out-of-the-box)

This solution builds and runs **without external NuGet dependencies** beyond .NET 8:

- `ResourceManager.Storage.Abstractions` — core types, telemetry scaffold, retry, JSON diff.
- `ResourceManager.Storage.Configuration` — DB registration, provider factory, config-aware manager, DI.
- `ResourceManager.Storage.Provider.FileSystem` — local FileSystem provider with sharded SHA-256 file names, ETag, optional AES-GCM encryption.
- `ResourceManager.Storage.KustoLoco` — simple table loader stub to stream/list data (placeholder for real Kusto-Loco eval).
- `ResourceManager.SampleApp` — console app showing end-to-end usage.

## Run
```bash
dotnet build ResourceManager.Storage.sln
dotnet run --project samples/ResourceManager.SampleApp/ResourceManager.SampleApp.csproj
```
This will create a `data/` folder under the app base directory and demonstrate create/get/update/list/query/delete.

## Enable encryption-at-rest
Update the registration:
```csharp
Encryption = new EncryptionOptions { Enabled = true, KeyBase64 = "<base64-32-byte-key>", KeyId = "k1" }
```
Use `openssl rand -base64 32` to generate a key.

## Tests
- [Orleans job scheduler tests](tests/Orleans.Jobs.Tests/README.md)
- [Orleans integration tests](tests/Orleans.Tests/README.md)
- [Storage provider contract tests](tests/Storage.Tests/README.md)

## Notes
- Additional providers (SQL, Azure Blob/Table, Cosmos) can be added by implementing `IDatabaseContext` and plugging into `StorageProviderFactory`.
- The Kusto loader currently scans via `ListAsync`. Plug your Kusto-Loco engine where indicated to evaluate KQL.
