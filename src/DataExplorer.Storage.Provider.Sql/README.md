# DataExplorer.Storage.Provider.Sql

## Purpose
Implements a SQL-based storage provider using relational tables and schemas.

## Public APIs
- `SqlStorageProvider` and `SqlDatabaseContext` for SQL database operations.
- `SqlOptions` to supply connection strings and schema names.
- `SqlProviderBuilder` and `RegistrationExtensions.AddSqlDatabase` for dependency injection.

## Build
```bash
dotnet build src/DataExplorer.Storage.Provider.Sql/Cloudbrick.DataExplorer.Storage.Provider.Sql.csproj
```

## Samples
- [Storage.SampleApp](../../samples/Storage.SampleApp)
