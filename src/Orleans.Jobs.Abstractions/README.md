# Orleans.Jobs.Abstractions

## Purpose
Defines the contracts and data models for distributed job scheduling and execution in Orleans.

## Public APIs
- Grain interfaces such as `IJobGrain`, `ITaskGrain`, `IJobsManagerGrain`, and `IScheduledJobsManagerGrain`.
- Manager interfaces `IJobsManager` and `IScheduledJobsManager`.
- Models including `JobSpec`, `TaskSpec`, `JobState`, `ExecutionEvent`, and related enums.

## Build
```bash
dotnet build src/Orleans.Jobs.Abstractions/Cloudbrick.Orleans.Jobs.Abstractions.csproj
```

## Samples
- [Orleans.Jobs.Playground](../../samples/Orleans.Jobs.Playground)
- [Components.Sample](../../samples/Components.Sample)
