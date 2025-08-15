# Cloudbrick.Components.Jobs

Blazor Razor Class Library with Fluent UI components for viewing and controlling Jobs/Tasks and live telemetry.

## Install

1. Add the project to your solution and reference it from your Blazor Server app.
2. In your Blazor app's `Program.cs`:

```csharp
builder.Services.AddCloudbrickJobsComponents(opts =>
{
    opts.ApiBaseUrl = "/api/jobs";        // your REST endpoints
    opts.TelemetryHubUrl = "/hubs/telemetry"; // SignalR hub for telemetry
});
builder.Services.AddFluentUIComponents();
```

3. Map the SignalR hub in your app (if you use the provided TelemetryHub):
```csharp
app.MapHub<TelemetryHub>("/hubs/telemetry");
```

## Use

```razor
@page "/jobs"
<JobList />

@page "/jobs/{id:guid}"
<JobDetail JobId="id" />
```

The library is backend-agnostic. Implement `IJobsBackend` against your APIs if they differ.
