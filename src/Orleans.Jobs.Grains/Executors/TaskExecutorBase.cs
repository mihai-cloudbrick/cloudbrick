using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Executors;

public abstract class TaskExecutorBase<TCommand> : ITaskExecutor
{
    public abstract string ExecutorType { get; }

    protected virtual JsonSerializerOptions SerializerOptions => new(JsonSerializerDefaults.Web);

    protected virtual Task ValidateAsync(TCommand cmd, ITaskExecutionContext ctx, CancellationToken ct) => Task.CompletedTask;
    protected abstract Task OnExecuteAsync(TCommand cmd, ITaskExecutionContext ctx, CancellationToken ct);
    protected virtual Task OnErrorAsync(Exception ex, TCommand cmd, ITaskExecutionContext ctx, CancellationToken ct) => Task.CompletedTask;
    protected virtual Task OnCompletedAsync(TCommand cmd, ITaskExecutionContext ctx, CancellationToken ct) => Task.CompletedTask;

    public async Task ValidateAsync(TaskSpec spec, ITaskExecutionContext ctx, CancellationToken ct)
    {
        var cmd = Deserialize(spec.CommandJson);
        await ValidateAsync(cmd, ctx, ct);
    }

    public async Task ExecuteAsync(ITaskExecutionContext ctx, CancellationToken ct)
    {
        var cmd = Deserialize(((TaskExecutionContext)ctx).CommandJson);
        try
        {
            await OnExecuteAsync(cmd, ctx, ct);
            await OnCompletedAsync(cmd, ctx, ct);
        }
        catch (Exception ex)
        {
            await OnErrorAsync(ex, cmd, ctx, ct);
            throw;
        }
    }

    public Task OnErrorAsync(Exception ex, ITaskExecutionContext ctx, CancellationToken ct) => Task.CompletedTask;
    public Task OnCompletedAsync(ITaskExecutionContext ctx, CancellationToken ct) => Task.CompletedTask;

    private TCommand Deserialize(string json) => JsonSerializer.Deserialize<TCommand>(json, SerializerOptions)!;
}

// Internal helper to access CommandJson in base class
internal sealed class TaskExecutionContext : ITaskExecutionContext
{
    public TaskExecutionContext(Guid jobId, string taskId, string correlationId, string commandJson,
        Microsoft.Extensions.Logging.ILogger logger, Func<int, string?, Task> progress,
        Func<ExecutionEvent, Task> emit, Func<Task> save,
        Func<CancellationToken, Task> waitIfPaused)
    {
        JobId = jobId;
        TaskId = taskId;
        CorrelationId = correlationId;
        CommandJson = commandJson;
        Logger = logger;
        _progress = progress;
        _emit = emit;
        _save = save;
        _wait = waitIfPaused;
    }

    public Guid JobId { get; }
    public string TaskId { get; }
    public string CorrelationId { get; }
    public string CommandJson { get; }
    public Microsoft.Extensions.Logging.ILogger Logger { get; }

    private readonly Func<int, string?, Task> _progress;
    private readonly Func<ExecutionEvent, Task> _emit;
    private readonly Func<Task> _save;
    private readonly Func<CancellationToken, Task> _wait;

    public Task ReportProgressAsync(int percent, string? message = null) => _progress(percent, message);
    public Task EmitTelemetryAsync(ExecutionEvent evt) => _emit(evt);
    public Task SaveStateAsync() => _save();
    public Task WaitIfPausedAsync(CancellationToken ct) => _wait(ct);
}
