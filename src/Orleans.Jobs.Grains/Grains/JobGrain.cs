using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using Cloudbrick.Orleans.Jobs.Abstractions;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cloudbrick.Orleans.Jobs.Infra;

namespace Cloudbrick.Orleans.Jobs.Grains;

internal class JobGrain : Grain, IJobGrain
{
    private readonly IPersistentState<JobState> _state;
    private readonly ILogger<JobGrain> _logger;
    private readonly ITelemetrySinkFactory _sinkFactory;
    private CancellationTokenSource? _schedulerCts;
    private IJobTelemetrySink? _sink;
    private IAsyncStream<ExecutionEvent>? _jobStream;
    private DateTimeOffset _lastSnapshotAt = DateTimeOffset.MinValue;
    private static readonly TimeSpan SnapshotMinInterval = TimeSpan.FromMilliseconds(1000);
    private IDisposable? _schedulerTimer;


    private readonly Dictionary<string, StreamSubscriptionHandle<ExecutionEvent>> _taskSubscriptions = new();

    public JobGrain(
        [PersistentState(stateName: "state", storageName: "Default")] IPersistentState<JobState> state,
        ILogger<JobGrain> logger,
        ITelemetrySinkFactory sinkFactory)
    {
        _state = state;
        _logger = logger;
        _sinkFactory = sinkFactory;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var provider = this.GetStreamProvider(StreamConstants.ProviderName);
        _jobStream = provider.GetStream<ExecutionEvent>(StreamId.Create(StreamConstants.JobNamespace, this.GetPrimaryKey().ToString()));
        if (!string.IsNullOrWhiteSpace(_state.State.TelemetryProviderKey))
        {
            _sink = _sinkFactory.Create(_state.State.TelemetryProviderKey!, this.GetPrimaryKey(), _state.State.CorrelationId);
        }
        return Task.CompletedTask;
    }
    // NEW: cancel background loop on deactivation
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _schedulerCts?.Cancel();
        _schedulerCts?.Dispose();
        _schedulerCts = null;
        _schedulerTimer?.Dispose();
        _schedulerTimer = null;
        return Task.CompletedTask;
    }
    public async Task SubmitAsync(JobSpec spec)
    {
        _state.State.JobId = this.GetPrimaryKey();
        _state.State.Name = spec.Name;
        _state.State.MaxDegreeOfParallelism = Math.Max(1, spec.MaxDegreeOfParallelism);
        _state.State.FailFast = spec.FailFast;
        _state.State.CorrelationId = spec.CorrelationId ?? Guid.NewGuid().ToString("N");
        _state.State.TelemetryProviderKey = spec.TelemetryProviderKey;

        // Build task states with dependencies
        foreach (var kvp in spec.Tasks)
        {
            var t = kvp.Value;
            t.TaskId = kvp.Key;
            t.CorrelationId ??= _state.State.CorrelationId;
            _state.State.Tasks[t.TaskId] = new TaskState
            {
                TaskId = t.TaskId,
                ExecutorType = t.ExecutorType,
                CommandJson = t.CommandJson,
                Dependencies = new List<string>(t.Dependencies ?? new List<string>()),
                Status = JobTaskStatus.Created,
                CreatedAt = DateTimeOffset.UtcNow,
                JobId = this.GetPrimaryKey().ToString(),
                CorrelationId = t.CorrelationId
            };
        }

        // Validate DAG
        var graph = spec.Tasks.ToDictionary(k => k.Key, v => v.Value.Dependencies ?? new List<string>());
        if (DagValidator.HasCycle(graph.ToDictionary(k => k.Key, k => k.Value.ToList())))
            throw new InvalidOperationException("Job has cyclic dependencies.");

        await _state.WriteWithRetry(_state.State.Clone());

        await RecalculateAndEmitSnapshotAsync(forcePersist: false, forceEmit: true);

        if (!string.IsNullOrWhiteSpace(spec.TelemetryProviderKey))
            _sink = _sinkFactory.Create(spec.TelemetryProviderKey!, this.GetPrimaryKey(), _state.State.CorrelationId);

        await EmitAsync(new ExecutionEvent { EventType = ExecutionEventType.StatusChanged, JobId = this.GetPrimaryKey(), Message = "Created", CorrelationId = _state.State.CorrelationId });
    }

    public async Task StartAsync()
    {
        if (_state.State.Status == JobStatus.Running) return;

        _state.State.Status = JobStatus.Running;
        _state.State.StartedAt = DateTimeOffset.UtcNow;
        await _state.WriteWithRetry(_state.State.Clone());
        await EmitAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.StatusChanged,
            JobId = this.GetPrimaryKey(),
            Message = "Running",
            CorrelationId = _state.State.CorrelationId
        });

        // FIX: create a fresh CTS for the scheduler and pass its token
        _schedulerCts?.Cancel();
        _schedulerCts?.Dispose();
        _schedulerCts = new CancellationTokenSource();

        _schedulerTimer?.Dispose();
        _schedulerTimer = this.RegisterGrainTimer<object>((state, cts) => TickSchedulerAsync(
            this.GetPrimaryKey(),
            _state.State.MaxDegreeOfParallelism,
            _state.State.FailFast,
            _schedulerCts.Token), state: null,
            dueTime: TimeSpan.Zero, period: TimeSpan.FromMilliseconds(500));


        await RecalculateAndEmitSnapshotAsync(forcePersist: false, forceEmit: true);
    }

    private async Task TickSchedulerAsync(Guid jobId, int mdop, bool failFast, CancellationToken ct)
    {
        if (_state.State.Status == JobStatus.Paused)
            return;

        if (_state.State.Status == JobStatus.Cancelling)
        {
            // optional: mark lingering Created tasks as Cancelled, then try finalize
            bool changed = false;
            foreach (var ts in _state.State.Tasks.Values)
            {
                if (ts.Status == JobTaskStatus.Created)
                {
                    ts.Status = JobTaskStatus.Cancelled;
                    ts.CompletedAt = DateTimeOffset.UtcNow;
                    changed = true;
                }
            }
            if (changed) await _state.WriteWithRetry(_state.State.Clone());
            await RecalculateAndEmitSnapshotAsync();
            await TryFinalizeIfTerminalAsync();
            return;
        }

        if (_state.State.Status == JobStatus.Cancelling)
        {
            // ensure any left-over Created tasks get cancelled
            var changed = false;
            foreach (var ts in _state.State.Tasks.Values)
            {
                if (ts.Status == JobTaskStatus.Created)
                {
                    ts.Status = JobTaskStatus.Cancelled;
                    ts.CompletedAt = DateTimeOffset.UtcNow;
                    changed = true;
                }
            }
            if (changed) await _state.WriteWithRetry(_state.State.Clone());
            await RecalculateAndEmitSnapshotAsync();
            await TryFinalizeIfTerminalAsync();
            return;
        }

        if (_state.State.Status != JobStatus.Running)
            return;

        var running = new HashSet<string>();

        ct.ThrowIfCancellationRequested();

        // Compute how many are already in-flight (Queued or Running)
        var inFlight = _state.State.Tasks.Values.Count(t =>
            t.Status == JobTaskStatus.Queued || t.Status == JobTaskStatus.Running);

        // Eligible ONLY if Created and dependencies succeeded
        var eligible = _state.State.Tasks.Values
            .Where(t => t.Status == JobTaskStatus.Created &&
                        t.Dependencies.All(d => _state.State.Tasks.TryGetValue(d, out var dep) &&
                                                 dep.Status == JobTaskStatus.Succeeded))
            .OrderBy(t => t.TaskId)
            .Select(t => t.TaskId)
            .ToList();

        foreach (var tid in eligible)
        {
            if (inFlight >= _state.State.MaxDegreeOfParallelism) break;

            // Subscribe BEFORE starting to avoid missing early events
            await SubscribeTaskTelemetryAsync(tid);

            // Mark as queued in JobState so next loop won't re-queue
            _state.State.Tasks[tid].Status = JobTaskStatus.Queued;
            await _state.WriteWithRetry(_state.State.Clone());

            await RecalculateAndEmitSnapshotAsync(); // throttled snapshot

            var taskSpec = GetTaskSpec(tid);
            await GrainFactory.GetGrain<ITaskGrain>(tid).StartAsync(jobId, taskSpec);

            inFlight++; // count it
        }

        // Terminal check unchanged...
        var allTerminal = _state.State.Tasks.Values.All(t =>
            t.Status is JobTaskStatus.Succeeded or JobTaskStatus.Failed or JobTaskStatus.Cancelled);
        if (allTerminal)
        {
            var anyFailed = _state.State.Tasks.Values.Any(t => t.Status == JobTaskStatus.Failed);
            var anyCancelled = _state.State.Tasks.Values.Any(t => t.Status == JobTaskStatus.Cancelled);
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            _state.State.Status = anyFailed ? JobStatus.Failed : anyCancelled ? JobStatus.Cancelled : JobStatus.Succeeded;
            await _state.WriteWithRetry(_state.State.Clone());
            await EmitAsync(new ExecutionEvent { EventType = ExecutionEventType.Completed, JobId = jobId, Message = _state.State.Status.ToString(), CorrelationId = _state.State.CorrelationId });
            _schedulerTimer?.Dispose();
            _schedulerTimer = null;
        }

        await RecalculateAndEmitSnapshotAsync(forcePersist: true, forceEmit: true);
    }

    private TaskSpec GetTaskSpec(string taskId)
    {
        var state = _state.State.Tasks[taskId];
        return new TaskSpec
        {
            TaskId = state.TaskId,
            ExecutorType = state.ExecutorType,
            CommandJson = state.CommandJson,
            Dependencies = new List<string>(state.Dependencies ?? new List<string>()),
            CorrelationId = state.CorrelationId
        };
    }

    private async Task SubscribeTaskTelemetryAsync(string taskId)
    {
        if (_taskSubscriptions.ContainsKey(taskId)) return;

        var provider = this.GetStreamProvider(StreamConstants.ProviderName);
        var stream = provider.GetStream<ExecutionEvent>(StreamId.Create(StreamConstants.TaskNamespace, taskId));

        StreamSubscriptionHandle<ExecutionEvent>? handle = null;

        handle = await stream.SubscribeAsync(
    async (evt, token) =>
    {
        // Normalize
        evt.JobId = this.GetPrimaryKey();
        evt.TaskId ??= taskId;
        evt.CorrelationId = _state.State.CorrelationId;

        if (_state.State.Tasks.TryGetValue(taskId, out var ts))
        {
            // Optional run metadata from recurring tasks
            if (evt.RunNumber.HasValue && evt.RunNumber.Value > ts.RunCount)
                ts.RunCount = evt.RunNumber.Value;
            if (evt.NextRunAt.HasValue)
                ts.NextRunAt = evt.NextRunAt;

            switch (evt.EventType)
            {
                case ExecutionEventType.StatusChanged:
                    switch (evt.Message)
                    {
                        case "Created": ts.Status = JobTaskStatus.Created; break;
                        case "Queued": ts.Status = JobTaskStatus.Queued; break;
                        case "Running": ts.Status = JobTaskStatus.Running; break;
                        case "Paused": ts.Status = JobTaskStatus.Paused; break;
                        case "Cancelled": ts.Status = JobTaskStatus.Cancelled; ts.CompletedAt = DateTimeOffset.UtcNow; break;
                        case "Scheduled": ts.Status = JobTaskStatus.Scheduled; break; // recurring idle
                    }
                    await _state.WriteWithRetry(_state.State.Clone());
                    break;

                case ExecutionEventType.Progress:
                    if (evt.Progress.HasValue) ts.Progress = evt.Progress.Value;
                    // no persistence for progress (avoid ETag churn)
                    break;

                case ExecutionEventType.Completed:
                    // Recurring tasks may flip back to Scheduled after a run
                    if (ts.IsRecurring && (!ts.MaxRuns.HasValue || ts.RunCount < ts.MaxRuns.Value))
                    {
                        ts.Status = JobTaskStatus.Scheduled;
                    }
                    else
                    {
                        ts.Status = JobTaskStatus.Succeeded;
                        ts.CompletedAt = DateTimeOffset.UtcNow;
                    }
                    await _state.WriteWithRetry(_state.State.Clone());
                    break;

                case ExecutionEventType.Error:
                    ts.Status = JobTaskStatus.Failed;
                    ts.CompletedAt = DateTimeOffset.UtcNow;
                    ts.LastError = evt.Exception ?? "Task failed.";
                    await _state.WriteWithRetry(_state.State.Clone());
                    break;

                case ExecutionEventType.JobSnapshot:
                    // ignore here; snapshots are emitted by JobGrain itself
                    break;
            }
        }

        // Update aggregate counters / % and (throttled) emit a JobSnapshot
        await RecalculateAndEmitSnapshotAsync();

        // Fan-out the original event to job stream + sinks
        await EmitAsync(evt);
    },
    async ex =>
    {
        _logger.LogWarning(ex, "Task stream '{TaskId}' failed; resubscribing…", taskId);
        _taskSubscriptions.Remove(taskId);
        await SubscribeTaskTelemetryAsync(taskId);
    },
    async () =>
    {
        _taskSubscriptions.Remove(taskId);
        await Task.CompletedTask;
    });

        _taskSubscriptions[taskId] = handle;
    }

    public async Task PauseAsync()
    {
        if (_state.State.Status is JobStatus.Paused or JobStatus.Cancelling) return;

        _state.State.Status = JobStatus.Paused;

        // Stop job scheduler
        _schedulerTimer?.Dispose();
        _schedulerTimer = null;

        // Ask every task to pause (Running/Queued/Created/Scheduled)
        foreach (var kv in _state.State.Tasks)
        {
            var ts = kv.Value;
            if (ts.Status is JobTaskStatus.Running
                         or JobTaskStatus.Queued
                         or JobTaskStatus.Created
                         or JobTaskStatus.Scheduled
                         or JobTaskStatus.Paused) // idempotent
            {
                await GrainFactory.GetGrain<ITaskGrain>(kv.Key).PauseAsync();
            }
        }

        await _state.WriteWithRetry(_state.State.Clone());

        // tell listeners the job is paused + refresh counters
        await EmitAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.StatusChanged,
            JobId = this.GetPrimaryKey(),
            Message = "Paused",
            CorrelationId = _state.State.CorrelationId
        });
        await RecalculateAndEmitSnapshotAsync(forcePersist: false, forceEmit: true);
    }

    public async Task ResumeAsync()
    {
        if (_state.State.Status != JobStatus.Paused) return;

        _state.State.Status = JobStatus.Running;
        await _state.WriteWithRetry(_state.State.Clone());

        // Resume tasks that are paused
        foreach (var kv in _state.State.Tasks)
        {
            var ts = kv.Value;
            if (ts.Status == JobTaskStatus.Paused)
                await GrainFactory.GetGrain<ITaskGrain>(kv.Key).ResumeAsync();
        }

        // Restart job scheduler (so Created tasks can be queued again)
        _schedulerTimer?.Dispose();
        _schedulerTimer = this.RegisterGrainTimer(() => TickSchedulerAsync(_state.State.JobId, _state.State.MaxDegreeOfParallelism, _state.State.FailFast, CancellationToken.None), TimeSpan.Zero, TimeSpan.FromMilliseconds(200));

        await EmitAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.StatusChanged,
            JobId = this.GetPrimaryKey(),
            Message = "Running",
            CorrelationId = _state.State.CorrelationId
        });
        await RecalculateAndEmitSnapshotAsync(forcePersist: false, forceEmit: true);
    }

    public async Task CancelAsync()
    {

        if(_state.State.Status is JobStatus.Cancelled or JobStatus.Failed or JobStatus.Succeeded) return;

        _state.State.Status = JobStatus.Cancelling;
        _schedulerTimer?.Dispose(); 
        _schedulerTimer = null;

        foreach (var kv in _state.State.Tasks)
        {
            var ts = kv.Value;
            switch (ts.Status)
            {
                case JobTaskStatus.Created:
                    ts.Status = JobTaskStatus.Cancelled;
                    ts.CompletedAt = DateTimeOffset.UtcNow;
                    await EmitAsync(new ExecutionEvent { EventType = ExecutionEventType.StatusChanged, JobId = this.GetPrimaryKey(), TaskId = ts.TaskId, Message = "Cancelled", CorrelationId = _state.State.CorrelationId });
                    break;

                case JobTaskStatus.Queued:
                case JobTaskStatus.Running:
                case JobTaskStatus.Paused:
                case JobTaskStatus.Cancelling:
                    // Stream signal + method fallback
                    await PublishTaskControlAsync(kv.Key, TaskControlAction.Cancel, "Job cancelled");
                    await GrainFactory.GetGrain<ITaskGrain>(kv.Key).CancelAsync();
                    break;
            }
        }

        await _state.WriteWithRetry(_state.State.Clone());
        await RecalculateAndEmitSnapshotAsync(forcePersist: true, forceEmit: true);
        await TryFinalizeIfTerminalAsync();
    }

    public async Task DeleteAsync()
{
    _schedulerCts?.Cancel();
    _schedulerCts?.Dispose();
    _schedulerCts = null;
    _schedulerTimer?.Dispose();
    _schedulerTimer = null;

    foreach (var handle in _taskSubscriptions.Values)
    {
        try { await handle.UnsubscribeAsync(); } catch { /* ignore */ }
    }
    _taskSubscriptions.Clear();

    await _state.ClearStateAsync();
    _sink = null;
    DeactivateOnIdle();
}

public Task<JobState?> GetStateAsync()
{
    if (!_state.RecordExists || _state.State.JobId == Guid.Empty)
        return Task.FromResult<JobState?>(null);
    return Task.FromResult<JobState?>(_state.State);
}

    public Task FlushAsync() => _state.WriteWithRetry(_state.State.Clone());

    public async Task EmitTelemetryAsync(ExecutionEvent evt)
    {
        evt.JobId = this.GetPrimaryKey();
        evt.CorrelationId = _state.State.CorrelationId;
        await EmitAsync(evt);
    }

    public Task SetTelemetryProviderAsync(string providerKey)
    {
        _state.State.TelemetryProviderKey = providerKey;
        _sink = _sinkFactory.Create(providerKey, this.GetPrimaryKey(), _state.State.CorrelationId);
        return _state.WriteStateAsync();
    }

    private async Task EmitAsync(ExecutionEvent evt)
    {
        if (_jobStream != null) await _jobStream.OnNextAsync(evt);
        if (_sink != null)
        {
            if (!string.IsNullOrWhiteSpace(evt.TaskId)) await _sink.OnTaskEventAsync(evt);
            else await _sink.OnJobEventAsync(evt);
        }
    }
    private async Task RecalculateAndEmitSnapshotAsync(bool forcePersist = false, bool forceEmit = false)
    {
        var tasks = _state.State.Tasks.Values;

        var total = tasks.Count;
        var running = tasks.Count(t => t.Status == JobTaskStatus.Running);
        var queued = tasks.Count(t => t.Status == JobTaskStatus.Queued || t.Status == JobTaskStatus.Created || t.Status == JobTaskStatus.Paused);
        var succeeded = tasks.Count(t => t.Status == JobTaskStatus.Succeeded);
        var failed = tasks.Count(t => t.Status == JobTaskStatus.Failed);
        var cancelled = tasks.Count(t => t.Status == JobTaskStatus.Cancelled);
        var completed = succeeded + failed + cancelled;
        var percent = total == 0 ? 100 : (int)Math.Round(100.0 * completed / total);

        var now = DateTimeOffset.UtcNow;
        var shouldEmit = forceEmit || now - _lastSnapshotAt >= SnapshotMinInterval;

        // update state snapshot
        _state.State.TotalTasks = total;
        _state.State.RunningTasks = running;
        _state.State.QueuedTasks = queued;
        _state.State.SucceededTasks = succeeded;
        _state.State.FailedTasks = failed;
        _state.State.CancelledTasks = cancelled;
        _state.State.CompletedTasks = completed;
        _state.State.JobProgress = percent;

        if (forcePersist || shouldEmit)
            await _state.WriteWithRetry(_state.State.Clone());



        if (shouldEmit)
        {
            _lastSnapshotAt = now;
            await EmitAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.JobSnapshot,
                JobId = this.GetPrimaryKey(),
                CorrelationId = _state.State.CorrelationId,
                Message = $"Job {percent}%",
                TotalTasks = total,
                RunningTasks = running,
                QueuedTasks = queued,
                SucceededTasks = succeeded,
                FailedTasks = failed,
                CancelledTasks = cancelled,
                CompletedTasks = completed,
                JobProgress = percent
            });
        }
    }

    private async Task TryFinalizeIfTerminalAsync()
    {
        var allTerminal = _state.State.Tasks.Values.All(t =>
            t.Status is JobTaskStatus.Succeeded or JobTaskStatus.Failed or JobTaskStatus.Cancelled);

        if (!allTerminal) return;

        var anyFailed = _state.State.Tasks.Values.Any(t => t.Status == JobTaskStatus.Failed);
        var anyCancelled = _state.State.Tasks.Values.Any(t => t.Status == JobTaskStatus.Cancelled);
        _state.State.CompletedAt = DateTimeOffset.UtcNow;
        _state.State.Status = anyFailed ? JobStatus.Failed
                               : anyCancelled ? JobStatus.Cancelled
                               : JobStatus.Succeeded;

        await _state.WriteStateAsync();
        await EmitAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.Completed,
            JobId = this.GetPrimaryKey(),
            Message = _state.State.Status.ToString(),
            CorrelationId = _state.State.CorrelationId
        });
    }

    private async Task PublishTaskControlAsync(string taskId, TaskControlAction action, string? reason = null)
    {
        var provider = this.GetStreamProvider(StreamConstants.ProviderName);
        var stream = provider.GetStream<TaskControlEvent>(
            StreamId.Create(StreamConstants.TaskControlNamespace, taskId));
        await stream.OnNextAsync(new TaskControlEvent { Action = action, Reason = reason });
    }

}
