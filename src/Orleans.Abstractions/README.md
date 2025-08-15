# Orleans.Abstractions

## Purpose
Contains helper types for Orleans integrations including JSON-based grain key formatting utilities.

## Public APIs
- `CompoundJsonKey`, `GrainKeyJsonHelper`, and `JsonKeyFormat` for building grain identifiers.
- `OrleansJsonKeyExtensions` with extensions for parsing and composing keys.

## Build
```bash
dotnet build src/Orleans.Abstractions/Cloudbrick.Orleans.Abstractions.csproj
```

## Samples
See [Orleans.Jobs.Playground](../../samples/Orleans.Jobs.Playground) for usage in a grain-based application.
