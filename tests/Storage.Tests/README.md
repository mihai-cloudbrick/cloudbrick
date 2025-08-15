# Storage.Tests

Contract tests for Cloudbrick DataExplorer storage providers.

## Scope
- Shared contract tests for CRUD, concurrency, and table lifecycle operations.
- Targets multiple providers: FileSystem, Azure Blob, Azure Table, Cosmos DB, and SQL Server.

## Running the tests
```bash
dotnet test tests/Storage.Tests/Cloudbrick.DataExplorer.Storage.Tests.csproj
```

### Provider-specific settings

| Provider | Environment variables | External dependency |
|----------|----------------------|---------------------|
| Azure Blob | `TEST_BLOB_CONN` | Azurite emulator or Azure Storage account |
| Azure Table | `TEST_TABLE_CONN` | Azurite emulator or Azure Storage account |
| Cosmos DB | `TEST_COSMOS_ENDPOINT`, `TEST_COSMOS_KEY` | Azure Cosmos DB Emulator or account |
| SQL Server | `TEST_SQL_CONN` | Accessible SQL Server database |
| FileSystem | _None_ | Uses local temp directory |

Tests for providers without the required environment variables are skipped.
