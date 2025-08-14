using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface ITaskExecutionContext
{
    Guid JobId { get; }
    string TaskId { get; }
    string CorrelationId { get; }
    ILogger Logger { get; }

    Task ReportProgressAsync(int percent, string? message = null);
    Task EmitTelemetryAsync(ExecutionEvent evt);
    Task SaveStateAsync();
    Task WaitIfPausedAsync(CancellationToken ct);
}
