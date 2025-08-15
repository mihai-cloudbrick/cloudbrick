# Orleans.SignalR

## Purpose
Integrates SignalR with Orleans so hub messages can be routed through grains.

## Public APIs
- `SiloExtensions.AddCloudbrickSignalR` and `MapCloudbrickSignalR` for configuring hubs.
- Grain types such as `HubGrain`, `HubPublisherGrain`, and `HubDirectoryGrain`.
- `DynamicHub` and helpers like `HubRelayHostedService` and `HubMessageSender`.

## Build
```bash
dotnet build src/Orleans.SignalR/Cloudbrick.Orleans.SignalR.csproj
```

## Samples
- [Components.Sample](../../samples/Components.Sample)
