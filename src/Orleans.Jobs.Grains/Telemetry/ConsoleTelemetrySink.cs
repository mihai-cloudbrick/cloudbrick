using System;
using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Telemetry;

public class ConsoleTelemetrySink : IJobTelemetrySink
{
    private readonly Guid _jobId;
    private readonly string _correlationId;
    private readonly string _name;

    public ConsoleTelemetrySink(Guid jobId, string correlationId, string name = "console")
    {
        _jobId = jobId;
        _correlationId = correlationId;
        _name = name;
    }

    public Task OnJobEventAsync(ExecutionEvent evt)
    {
        Console.WriteLine($"[{_name}][{_correlationId}][JOB {evt.JobId}] {evt.EventType} {evt.Message}");
        return Task.CompletedTask;
    }

    public Task OnTaskEventAsync(ExecutionEvent evt)
    {
        Console.WriteLine($"[{_name}][{_correlationId}][TASK {evt.TaskId}] {evt.EventType} {evt.Message} {(evt.Progress.HasValue ? evt.Progress.Value + "%" : string.Empty)}");
        return Task.CompletedTask;
    }
}

public sealed class NoopTelemetrySink : IJobTelemetrySink
{
    public Task OnJobEventAsync(ExecutionEvent evt) => Task.CompletedTask;
    public Task OnTaskEventAsync(ExecutionEvent evt) => Task.CompletedTask;
}

public class TelemetrySinkFactory : ITelemetrySinkFactory
{
    public IJobTelemetrySink Create(string providerKey, Guid jobId, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(providerKey) ||
            providerKey.Equals("none", StringComparison.OrdinalIgnoreCase))
            return new NoopTelemetrySink();

        if (providerKey.Equals("console", StringComparison.OrdinalIgnoreCase))
            return new ConsoleTelemetrySink(jobId, correlationId, "console");

        // add other providers here...
        return new NoopTelemetrySink();
    }
}
