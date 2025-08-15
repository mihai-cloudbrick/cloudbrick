# DataExplorer.Storage.Provider.FileSystem

## Purpose
Provides a file system based storage provider with optional AES-GCM encryption and sharded file layout.

## Public APIs
- `FileSystemStorageProvider` and `FileSystemDatabaseContext` for local disk storage.
- `FileSystemOptions` and `EncryptionOptions` to configure paths and at-rest encryption.
- `FileSystemProviderBuilder` and `RegistrationExtensions.AddLocalFileSystemDatabase` for DI registration.

## Build
```bash
dotnet build src/DataExplorer.Storage.Provider.FileSystem/Cloudbrick.DataExplorer.Storage.Provider.FileSystem.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
