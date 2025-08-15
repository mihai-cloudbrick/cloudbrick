using Cloudbrick.Orleans.Jobs.Abstractions;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cloudbrick.Orleans.Jobs.Executors;
using Cloudbrick.Orleans.Jobs.Infra;
using Cronos;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;
using Orleans.Streams;

namespace Cloudbrick.Orleans.Jobs.Grains;
[Reentrant]
public class TaskGrain : Grain, ITaskGrain
{
    private readonly IPersistentState<TaskState> _state;
    private readonly ILogger<TaskGrain> _logger;
    private readonly ITaskExecutorFactory _executorFactory;
    private IDisposable? _cancelWatchdog;
    private CancellationTokenSource? _cts;
    private PauseGate _pause = new();
    private IDisposable? _keepAliveTimer;
    private IAsyncStream<ExecutionEvent>? _taskStream;
    private IAsyncStream<TaskControlEvent>? _controlStream;
    private StreamSubscriptionHandle<TaskControlEvent>? _controlHandle;
    private IDisposable? _cronTimer;
    private CronExpression? _cronExpr;
    private TimeZoneInfo _cronTz = TimeZoneInfo.Utc;
    private bool _isExecuting;
    public TaskGrain(
        [PersistentState(stateName: "state", storageName: "Default")] IPersistentState<TaskState> state,
        ILogger<TaskGrain> logger,
        ITaskExecutorFactory executorFactory)
    {
        _state = state;
        _logger = logger;
        _executorFactory = executorFactory;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _keepAliveTimer = this.RegisterGrainTimer<object?>((state, cxt) =>
        {
            DelayDeactivation(TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

        var provider = this.GetStreamProvider(StreamConstants.ProviderName);
        _taskStream = provider.GetStream<ExecutionEvent>(StreamId.Create(StreamConstants.TaskNamespace, this.GetPrimaryKeyString()));

        _controlStream = provider.GetStream<TaskControlEvent>(
        StreamId.Create(StreamConstants.TaskControlNamespace, this.GetPrimaryKeyString()));

        _controlHandle = await _controlStream.SubscribeAsync(async (evt, ct) =>
        {
            switch (evt.Action)
            {
                case TaskControlAction.Pause: await PauseAsync(); break;
                case TaskControlAction.Resume: await ResumeAsync(); break;
                case TaskControlAction.Cancel: await CancelAsync(); break;
            }
        });
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        if (_controlHandle is not null) 
        { 
            try 
            { 
                await _controlHandle.UnsubscribeAsync(); 
            } catch { } 
        }
        _keepAliveTimer?.Dispose();
        await base.OnDeactivateAsync(reason,cancellationToken);
    }

    public async Task StartAsync(Guid jobId, TaskSpec spec)
    {
        // NEW: ignore if not in a startable state
        if (_state.State.Status is JobTaskStatus.Queued
            or JobTaskStatus.Running
            or JobTaskStatus.Paused
            or JobTaskStatus.Cancelling
            or JobTaskStatus.Cancelled
            or JobTaskStatus.Succeeded
            or JobTaskStatus.Failed)
        {
            return;
        }


        if (string.IsNullOrWhiteSpace(spec.TaskId))
            spec.TaskId = this.GetPrimaryKeyString();

        if (_state.State.Status is JobTaskStatus.Running or JobTaskStatus.Succeeded or JobTaskStatus.Failed)
            return;

        _state.State.TaskId = spec.TaskId;
        _state.State.JobId = jobId.ToString();
        _state.State.ExecutorType = spec.ExecutorType;
        _state.State.CommandJson = spec.CommandJson;
        _state.State.Dependencies = new List<string>(spec.Dependencies ?? new List<string>());
        _state.State.CorrelationId = spec.CorrelationId ?? Guid.NewGuid().ToString("N");
        _state.State.Status = JobTaskStatus.Queued;

        _state.State.Cron = spec.Cron;
        _state.State.CronTimeZone = spec.CronTimeZone;
        _state.State.AllowConcurrentRuns = spec.AllowConcurrentRuns;
        _state.State.MaxRuns = spec.MaxRuns;
        _state.State.NotBefore = spec.NotBefore;
        _state.State.NotAfter = spec.NotAfter;
        _state.State.IsRecurring = !string.IsNullOrWhiteSpace(spec.Cron);

        if (_state.State.IsRecurring)
        {
            // Parse CRON (detect 5 vs 6 fields)
            var parts = spec.Cron!.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var format = parts.Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;
            _cronExpr = CronExpression.Parse(spec.Cron!, format);

            _cronTz = string.IsNullOrWhiteSpace(spec.CronTimeZone)
                ? TimeZoneInfo.Utc
                : TimeZoneInfo.FindSystemTimeZoneById(spec.CronTimeZone!);

            _state.State.Status = JobTaskStatus.Scheduled; // idle until next fire
            _state.State.StartedAt ??= DateTimeOffset.UtcNow;
            await _state.WriteStateAsync();

            await EmitAsync(new ExecutionEvent
            {
                JobId = jobId,
                TaskId = _state.State.TaskId,
                EventType = ExecutionEventType.StatusChanged,
                Message = "Scheduled",
                CorrelationId = _state.State.CorrelationId
            });

            ScheduleNextCronTick(DateTimeOffset.UtcNow);
            return; // do NOT fall into one-shot execution below
        }

        await _state.WriteStateAsync();

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var provider = this.GetStreamProvider(StreamConstants.ProviderName);

        await EmitAsync(new ExecutionEvent
        {
            JobId = jobId,
            TaskId = _state.State.TaskId,
            EventType = ExecutionEventType.StatusChanged,
            Message = "Queued",
            CorrelationId = _state.State.CorrelationId
        });

        _ = RunAsync(jobId, spec, _cts.Token);
    }
    private void ScheduleNextCronTick(DateTimeOffset now)
    {
        if (_cronExpr == null) return;

        if (_state.State.NotAfter.HasValue && now >= _state.State.NotAfter.Value)
        {
            // End of schedule: finalize as Succeeded (completed its schedule window)
            _cronTimer?.Dispose(); _cronTimer = null;
            _state.State.Status = JobTaskStatus.Succeeded;
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            _ = _state.WriteStateAsync();
            _ = EmitAsync(new ExecutionEvent
            {
                JobId = Guid.Parse(_state.State.JobId),
                TaskId = _state.State.TaskId,
                EventType = ExecutionEventType.Completed,
                Message = "Schedule window ended",
                CorrelationId = _state.State.CorrelationId
            });
            return;
        }

        var anchor = now;
        if (_state.State.NotBefore.HasValue && _state.State.RunCount == 0 && anchor < _state.State.NotBefore.Value)
            anchor = _state.State.NotBefore.Value;

        var next = _cronExpr.GetNextOccurrence(anchor.UtcDateTime, _cronTz);
        if (next == null)
        {
            // No more occurrences — finalize as Succeeded
            _cronTimer?.Dispose(); _cronTimer = null;
            _state.State.Status = JobTaskStatus.Succeeded;
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            _ = _state.WriteStateAsync();
            _ = EmitAsync(new ExecutionEvent
            {
                JobId = Guid.Parse(_state.State.JobId),
                TaskId = _state.State.TaskId,
                EventType = ExecutionEventType.Completed,
                Message = "No further CRON occurrences",
                CorrelationId = _state.State.CorrelationId
            });
            return;
        }

        var nextLocal = new DateTimeOffset(next.Value, _cronTz.GetUtcOffset(next.Value));
        _state.State.NextRunAt = nextLocal;
        _ = _state.WriteStateAsync();

        var due = nextLocal - DateTimeOffset.UtcNow;
        if (due < TimeSpan.Zero) due = TimeSpan.Zero;

        _cronTimer?.Dispose();
        _cronTimer = this.RegisterGrainTimer(_ => CronFireAsync(), due, Timeout.InfiniteTimeSpan);

        // optional: emit upcoming run as telemetry
        _ = EmitAsync(new ExecutionEvent
        {
            JobId = Guid.Parse(_state.State.JobId),
            TaskId = _state.State.TaskId,
            EventType = ExecutionEventType.Custom,
            Message = $"Next run at {_state.State.NextRunAt:O}",
            CorrelationId = _state.State.CorrelationId,
            NextRunAt = _state.State.NextRunAt
        });
    }
    private async Task CronFireAsync()
    {
        if (_state.State.Status is JobTaskStatus.Cancelling or JobTaskStatus.Cancelled)
            return;

        if (!_state.State.AllowConcurrentRuns && _isExecuting)
        {
            // Skip overlapping run, reschedule next occurrence
            ScheduleNextCronTick(DateTimeOffset.UtcNow);
            return;
        }

        _isExecuting = true;
        try
        {
            // Mark running (transient status for the run)
            _state.State.Status = JobTaskStatus.Running;
            _state.State.StartedAt = DateTimeOffset.UtcNow;
            _state.State.RunCount++;
            await _state.WriteStateAsync();

            await EmitAsync(new ExecutionEvent
            {
                JobId = Guid.Parse(_state.State.JobId),
                TaskId = _state.State.TaskId,
                EventType = ExecutionEventType.StatusChanged,
                Message = "Running",
                CorrelationId = _state.State.CorrelationId,
                RunNumber = _state.State.RunCount
            });

            // Build context + execute (reuse your existing executor flow)
            var spec = new TaskSpec
            {
                TaskId = _state.State.TaskId,
                ExecutorType = _state.State.ExecutorType,
                CommandJson = _state.State.CommandJson
            };
            var exec = _executorFactory.Resolve(spec.ExecutorType);

            var ctx = new TaskExecutionContext(
                Guid.Parse(_state.State.JobId),
                spec.TaskId,
                _state.State.CorrelationId,
                spec.CommandJson,
                _logger,
                progress: async (p, m) =>
                {
                    _state.State.Progress = Math.Clamp(p, 0, 100);
                    if (!string.IsNullOrWhiteSpace(m))
                        _state.State.History.Add(new TaskHistoryEntry { Timestamp = DateTimeOffset.UtcNow, Message = m });
                    // (throttled persist logic you already added is fine)
                    await EmitAsync(new ExecutionEvent
                    {
                        JobId = Guid.Parse(_state.State.JobId),
                        TaskId = spec.TaskId,
                        EventType = ExecutionEventType.Progress,
                        Progress = _state.State.Progress,
                        Message = m ?? string.Empty,
                        CorrelationId = _state.State.CorrelationId,
                        RunNumber = _state.State.RunCount
                    });
                },
                emit: async (evt) =>
                {
                    evt.CorrelationId = _state.State.CorrelationId;
                    evt.JobId = Guid.Parse(_state.State.JobId);
                    evt.TaskId = spec.TaskId;
                    evt.RunNumber ??= _state.State.RunCount;
                    await EmitAsync(evt);
                },
                save: async () => await _state.WriteStateAsync(),
                waitIfPaused: async (token) => await _pause.WaitAsync(token));

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts?.Token ?? CancellationToken.None);
            var ct = linked.Token;

            await exec.ValidateAsync(spec, ctx, ct);
            await RetryPolicy.ExecuteAsync(async attempt =>
            {
                _state.State.Attempts = attempt;
                await _state.WriteStateAsync();
                await exec.ExecuteAsync(ctx, ct);
            }, spec.MaxRetries, spec.RetryBackoffSeconds, ct);

            _state.State.LastRunAt = DateTimeOffset.UtcNow;

            // If recurring continues: go back to Scheduled; else finalize
            var stopRecurring = _state.State.MaxRuns.HasValue && _state.State.RunCount >= _state.State.MaxRuns.Value;
            if (stopRecurring)
            {
                _state.State.Status = JobTaskStatus.Succeeded;
                _state.State.CompletedAt = _state.State.LastRunAt;
            }
            else
            {
                _state.State.Status = JobTaskStatus.Scheduled;
                _state.State.Progress = 0; // reset between runs (optional)
            }

            await _state.WriteStateAsync();
            await exec.OnCompletedAsync(ctx, ct);
            await EmitAsync(new ExecutionEvent
            {
                JobId = Guid.Parse(_state.State.JobId),
                TaskId = spec.TaskId,
                EventType = ExecutionEventType.Completed,
                Message = stopRecurring ? "Completed (max runs reached)" : "Run completed",
                CorrelationId = _state.State.CorrelationId,
                RunNumber = _state.State.RunCount
            });

            if (!stopRecurring)
                ScheduleNextCronTick(DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
            _state.State.Status = JobTaskStatus.Cancelled;
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            await _state.WriteStateAsync();
            await EmitAsync(new ExecutionEvent
            {
                JobId = Guid.Parse(_state.State.JobId),
                TaskId = _state.State.TaskId,
                EventType = ExecutionEventType.StatusChanged,
                Message = "Cancelled",
                CorrelationId = _state.State.CorrelationId
            });
        }
        catch (Exception ex)
        {
            _state.State.LastError = ex.ToString();
            _state.State.LastRunAt = DateTimeOffset.UtcNow;

            // Either keep scheduling (default) or stop on error if you add a FailFast flag.
            _state.State.Status = JobTaskStatus.Scheduled;
            await _state.WriteStateAsync();

            try { await _executorFactory.Resolve(_state.State.ExecutorType).OnErrorAsync(ex, null!, CancellationToken.None); } catch { }
            await EmitAsync(new ExecutionEvent
            {
                JobId = Guid.Parse(_state.State.JobId),
                TaskId = _state.State.TaskId,
                EventType = ExecutionEventType.Error,
                Message = "Run failed",
                Exception = ex.Message,
                CorrelationId = _state.State.CorrelationId,
                RunNumber = _state.State.RunCount
            });

            // Continue scheduling next occurrences:
            ScheduleNextCronTick(DateTimeOffset.UtcNow);
        }
        finally
        {
            _isExecuting = false;
        }
    }
    private async Task RunAsync(Guid jobId, TaskSpec spec, CancellationToken ct)
    {
        _state.State.Status = JobTaskStatus.Running;
        _state.State.StartedAt = DateTimeOffset.UtcNow;
        await _state.WriteStateAsync();
        await EmitAsync(new ExecutionEvent { JobId = jobId, TaskId = spec.TaskId, EventType = ExecutionEventType.StatusChanged, Message = "Running", CorrelationId = _state.State.CorrelationId });

        var exec = _executorFactory.Resolve(spec.ExecutorType);
        var ctx = new TaskExecutionContext(jobId, spec.TaskId, _state.State.CorrelationId, spec.CommandJson, _logger,
            progress: async (p, m) =>
            {
                _state.State.Progress = Math.Clamp(p, 0, 100);
                if (!string.IsNullOrWhiteSpace(m))
                    _state.State.History.Add(new TaskHistoryEntry { Timestamp = DateTimeOffset.UtcNow, Message = m });
                await _state.WriteStateAsync();
                await EmitAsync(new ExecutionEvent { JobId = jobId, TaskId = spec.TaskId, EventType = ExecutionEventType.Progress, Progress = _state.State.Progress, Message = m ?? string.Empty, CorrelationId = _state.State.CorrelationId });
            },
            emit: async (evt) =>
            {
                evt.CorrelationId = _state.State.CorrelationId;
                evt.JobId = jobId;
                evt.TaskId = spec.TaskId;
                await EmitAsync(evt);
            },
            save: async () => await _state.WriteStateAsync(),
            waitIfPaused: async (token) => await _pause.WaitAsync(token));

        try
        {
            await exec.ValidateAsync(spec, ctx, ct);

            await RetryPolicy.ExecuteAsync(async attempt =>
            {
                _state.State.Attempts = attempt;
                await _state.WriteStateAsync();
                await exec.ExecuteAsync(ctx, ct);
            }, spec.MaxRetries, spec.RetryBackoffSeconds, ct);

            _state.State.Status = JobTaskStatus.Succeeded;
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            await _state.WriteStateAsync();
            await exec.OnCompletedAsync(ctx, ct);
            await EmitAsync(new ExecutionEvent { JobId = jobId, TaskId = spec.TaskId, EventType = ExecutionEventType.Completed, Message = "Succeeded", CorrelationId = _state.State.CorrelationId });
        }
        catch (OperationCanceledException)
        {
            _state.State.Status = JobTaskStatus.Cancelled;
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            await _state.WriteStateAsync();
            await EmitAsync(new ExecutionEvent { JobId = jobId, TaskId = spec.TaskId, EventType = ExecutionEventType.StatusChanged, Message = "Cancelled", CorrelationId = _state.State.CorrelationId });
        }
        catch (Exception ex)
        {
            _state.State.Status = JobTaskStatus.Failed;
            _state.State.LastError = ex.ToString();
            _state.State.CompletedAt = DateTimeOffset.UtcNow;
            await _state.WriteStateAsync();
            try { await _executorFactory.Resolve(spec.ExecutorType).OnErrorAsync(ex, ctx, ct); } catch {}
            await EmitAsync(new ExecutionEvent { JobId = jobId, TaskId = spec.TaskId, EventType = ExecutionEventType.Error, Message = "Failed", Exception = ex.Message, CorrelationId = _state.State.CorrelationId });
        }
    }

    public async Task PauseAsync()
    {
        if (_state.State.IsRecurring && _state.State.Status == JobTaskStatus.Scheduled)
        {
            _cronTimer?.Dispose();
            _cronTimer = null;
        }

        if (_state.State.Status is JobTaskStatus.Succeeded
        or JobTaskStatus.Failed
        or JobTaskStatus.Cancelled
        or JobTaskStatus.Cancelling)
            return;



        if (_state.State.Status == JobTaskStatus.Paused)
            return;

        _state.State.PausedFrom = _state.State.Status;

        _state.State.Status = JobTaskStatus.Paused;
        _pause.Pause();

        await _state.WriteStateAsync();
        await EmitAsync(new ExecutionEvent
        {
            JobId = Guid.Parse(_state.State.JobId),
            TaskId = _state.State.TaskId,
            EventType = ExecutionEventType.StatusChanged,
            Message = "Paused",
            CorrelationId = _state.State.CorrelationId
        });
    }

    public async Task ResumeAsync()
    {
        // Only act if actually paused
        if (_state.State.Status != JobTaskStatus.Paused)
            return;

        // Decide what to resume to (default to Running)
        var resumeTo = _state.State.PausedFrom ?? JobTaskStatus.Running;

        _state.State.PausedFrom = null;
        _state.State.Status = resumeTo;

        // Open the gate
        _pause.Resume();

        await _state.WriteStateAsync();

        // Emit the *correct* status message for UIs/aggregator
        var msg = resumeTo switch
        {
            JobTaskStatus.Scheduled => "Scheduled", // CRON task was idle between runs
            JobTaskStatus.Queued => "Queued",    // was enqueued but not started
            JobTaskStatus.Created => "Created",   // not yet enqueued
            _ => "Running"    // default (mid-run pause)
        };

        await EmitAsync(new ExecutionEvent
        {
            JobId = Guid.Parse(_state.State.JobId),
            TaskId = _state.State.TaskId,
            EventType = ExecutionEventType.StatusChanged,
            Message = msg,
            CorrelationId = _state.State.CorrelationId
        });
    }

    public async Task CancelAsync()
    {
        _cronTimer?.Dispose(); 
        _cronTimer = null;
        switch (_state.State.Status)
        {
            case JobTaskStatus.Created:
            case JobTaskStatus.Queued:
                _state.State.Status = JobTaskStatus.Cancelled;
                _state.State.CompletedAt = DateTimeOffset.UtcNow;
                await _state.WriteStateAsync();
                await EmitAsync(new ExecutionEvent { JobId = Guid.Parse(_state.State.JobId), TaskId = _state.State.TaskId, EventType = ExecutionEventType.StatusChanged, Message = "Cancelled", CorrelationId = _state.State.CorrelationId });
                return;

            case JobTaskStatus.Paused:
            case JobTaskStatus.Running:
            case JobTaskStatus.Cancelling:
                _state.State.Status = JobTaskStatus.Cancelling;
                _cts?.Cancel();
                _pause.Resume(); // unblock WaitIfPausedAsync
                await _state.WriteStateAsync();

                // start/refresh watchdog (e.g., 15s)
                _cancelWatchdog?.Dispose();
                _cancelWatchdog = this.RegisterGrainTimer(async () =>
                {
                    if (_state.State.Status == JobTaskStatus.Cancelling)
                    {
                        _state.State.Status = JobTaskStatus.Cancelled;
                        _state.State.CompletedAt = DateTimeOffset.UtcNow;
                        await _state.WriteStateAsync();
                        await EmitAsync(new ExecutionEvent { JobId = Guid.Parse(_state.State.JobId), TaskId = _state.State.TaskId, EventType = ExecutionEventType.StatusChanged, Message = "Cancelled", CorrelationId = _state.State.CorrelationId });
                    }
                }, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
                return;

            default:
                return;
        }
    }

    public Task<TaskState> GetStateAsync() => Task.FromResult(_state.State);

    public Task FlushAsync() => _state.WriteStateAsync();

    public async Task EmitTelemetryAsync(ExecutionEvent evt)
    {
        evt.CorrelationId = _state.State.CorrelationId;
        evt.JobId = Guid.Parse(_state.State.JobId);
        evt.TaskId = _state.State.TaskId;
        await EmitAsync(evt);
    }

    private async Task EmitAsync(ExecutionEvent evt)
    {
        if (_taskStream != null) await _taskStream.OnNextAsync(evt);
    }
}
