# Orleans.Jobs.Tests

These tests cover the Cloudbrick Orleans job system.

## Scope
- Validate job execution features such as cancellation, retries, and pause/resume behavior.
- Uses an in-memory Orleans `TestCluster` with memory storage and streams.

## Running the tests
```bash
dotnet test tests/Orleans.Jobs.Tests/Orleans.Jobs.Tests.csproj
```
No additional environment variables or external services are required.
