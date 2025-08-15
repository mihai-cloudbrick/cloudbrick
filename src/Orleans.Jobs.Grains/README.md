# Orleans.Jobs.Grains

## Purpose
Provides grain implementations and supporting infrastructure for running distributed jobs and tasks with Orleans.

## Public APIs
- Grain types such as `JobGrain`, `TaskGrain`, `JobsManagerGrain`, and scheduled job grains.
- `JobsSiloBuilderExtensions` to register job services with an Orleans silo.
- Executor and telemetry components including `ITaskExecutor` implementations and `TelemetryHub`.

## Build
```bash
dotnet build src/Orleans.Jobs.Grains/Cloudbrick.Orleans.Jobs.csproj
```

## Samples
- [Orleans.Jobs.Playground](../../samples/Orleans.Jobs.Playground)
- [Components.Sample](../../samples/Components.Sample)
