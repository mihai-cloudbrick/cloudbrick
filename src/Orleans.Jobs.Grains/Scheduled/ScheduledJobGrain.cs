using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cronos;
using Orleans.Concurrency;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Scheduled
{
    [Reentrant]
    public class ScheduledJobGrain : Grain, IScheduledJobGrain
    {
        private readonly IPersistentState<ScheduledJobState> _state;
        private IDisposable? _timer;
        private CronExpression? _cron;
        private TimeZoneInfo _tz = TimeZoneInfo.Utc;
        private bool _tickInProgress;

        public ScheduledJobGrain([PersistentState("schedJob", "Default")] IPersistentState<ScheduledJobState> state)
        {
            _state = state;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_state.State.Cron))
            {
                var parts = _state.State.Cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var fmt = parts.Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;
                _cron = CronExpression.Parse(_state.State.Cron, fmt);
                _tz = TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(_state.State.CronTimeZone) ? "UTC" : _state.State.CronTimeZone);
            }
            if (_state.State.Status == ScheduledJobStatus.Enabled)
            {
                ScheduleNext(DateTimeOffset.UtcNow);
            }
            return Task.CompletedTask;
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _timer?.Dispose(); _timer = null;
            return Task.CompletedTask;
        }

        public async Task ConfigureAsync(ScheduledJobSpec spec)
        {
            _state.State.TemplateId = string.IsNullOrWhiteSpace(spec.TemplateId) ? this.GetPrimaryKeyString() : spec.TemplateId;
            _state.State.Job = spec.Job;
            _state.State.Cron = spec.Cron;
            _state.State.CronTimeZone = spec.CronTimeZone ?? "UTC";
            _state.State.AllowOverlappingJobs = spec.AllowOverlappingJobs;
            _state.State.MaxRuns = spec.MaxRuns;
            _state.State.NotBefore = spec.NotBefore;
            _state.State.NotAfter = spec.NotAfter;
            _state.State.Status = spec.Status;

            var parts = spec.Cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var fmt = parts.Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;
            _cron = CronExpression.Parse(spec.Cron, fmt);
            _tz = TimeZoneInfo.FindSystemTimeZoneById(_state.State.CronTimeZone);

            await _state.WriteStateAsync();

            if (_state.State.Status == ScheduledJobStatus.Enabled)
                ScheduleNext(DateTimeOffset.UtcNow);
        }

        public Task EnableAsync()
        {
            _state.State.Status = ScheduledJobStatus.Enabled;
            ScheduleNext(DateTimeOffset.UtcNow);
            return _state.WriteStateAsync();
        }
        public Task DisableAsync()
        {
            _state.State.Status = ScheduledJobStatus.Disabled;
            _timer?.Dispose(); _timer = null;
            return _state.WriteStateAsync();
        }

        public Task PauseAsync()
        {
            _state.State.Status = ScheduledJobStatus.Paused;
            _timer?.Dispose(); _timer = null;
            return _state.WriteStateAsync();
        }

        public Task ResumeAsync()
        {
            _state.State.Status = ScheduledJobStatus.Enabled;
            ScheduleNext(DateTimeOffset.UtcNow);
            return _state.WriteStateAsync();
        }

        public Task<ScheduledJobState> GetStateAsync() => Task.FromResult(_state.State);

        public async Task UpdateSpecAsync(ScheduledJobSpec spec)
        {
            if (!string.IsNullOrWhiteSpace(spec.Cron))
            {
                var parts = spec.Cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var fmt = parts.Length == 6 ? CronFormat.IncludeSeconds : CronFormat.Standard;
                _cron = CronExpression.Parse(spec.Cron, fmt);
                _state.State.Cron = spec.Cron;
            }
            if (!string.IsNullOrWhiteSpace(spec.CronTimeZone))
            {
                _tz = TimeZoneInfo.FindSystemTimeZoneById(spec.CronTimeZone);
                _state.State.CronTimeZone = spec.CronTimeZone;
            }
            _state.State.AllowOverlappingJobs = spec.AllowOverlappingJobs;
            _state.State.MaxRuns = spec.MaxRuns;
            _state.State.NotBefore = spec.NotBefore;
            _state.State.NotAfter = spec.NotAfter;
            if (spec.Job is not null) _state.State.Job = spec.Job;

            await _state.WriteStateAsync();

            if (_state.State.Status == ScheduledJobStatus.Enabled)
            {
                _timer?.Dispose();
                ScheduleNext(DateTimeOffset.UtcNow);
            }
        }

        public Task RunNowAsync()
        {
            _ = FireAsync();
            return Task.CompletedTask;
        }

        public async Task DeleteAsync()
        {
            _timer?.Dispose(); _timer = null;
            await _state.ClearStateAsync();
            DeactivateOnIdle();
        }

        private void ScheduleNext(DateTimeOffset now)
        {
            if (_cron == null) return;

            if (_state.State.NotAfter.HasValue && now >= _state.State.NotAfter.Value)
            {
                _timer?.Dispose(); _timer = null;
                return;
            }

            var anchor = now;
            if (_state.State.NotBefore.HasValue && _state.State.RunCount == 0 && anchor < _state.State.NotBefore.Value)
                anchor = _state.State.NotBefore.Value;

            var next = _cron.GetNextOccurrence(anchor.UtcDateTime, _tz);
            if (next == null) { _timer?.Dispose(); _timer = null; return; }

            var nextLocal = new DateTimeOffset(next.Value, _tz.GetUtcOffset(next.Value));
            _state.State.NextRunAt = nextLocal;
            _ = _state.WriteStateAsync();

            var due = nextLocal - DateTimeOffset.UtcNow;
            if (due < TimeSpan.Zero) due = TimeSpan.Zero;

            _timer?.Dispose();
            _timer = this.RegisterGrainTimer(() => FireAsync(),  due, Timeout.InfiniteTimeSpan);
        }

        private async Task FireAsync()
        {
            if (_tickInProgress) return;
            _tickInProgress = true;
            try
            {
                if (_state.State.Status != ScheduledJobStatus.Enabled)
                    return;

                if (_state.State.MaxRuns.HasValue && _state.State.RunCount >= _state.State.MaxRuns.Value)
                {
                    _timer?.Dispose(); _timer = null;
                    return;
                }

                if (!_state.State.AllowOverlappingJobs && _state.State.LastJobId.HasValue)
                {
                    var mgr = GrainFactory.GetGrain<IJobsManagerGrain>("manager");
                    var last = await mgr.GetJobStateAsync(_state.State.LastJobId.Value);
                    if (last.Status is not JobStatus.Succeeded
                        and not JobStatus.Failed
                        and not JobStatus.Cancelled)
                    {
                        ScheduleNext(DateTimeOffset.UtcNow);
                        return;
                    }
                }

                var jobSpec = CloneJobSpec(_state.State.Job);
                jobSpec.CorrelationId = $"{_state.State.TemplateId}:{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
                if (string.IsNullOrWhiteSpace(jobSpec.TelemetryProviderKey))
                    jobSpec.TelemetryProviderKey = "console";

                var manager = GrainFactory.GetGrain<IJobsManagerGrain>("manager");
                var jobId = await manager.CreateJobAsync(jobSpec);
                await manager.StartJobAsync(jobId);

                _state.State.LastJobId = jobId;
                _state.State.RecentJobIds.Add(jobId);
                if (_state.State.RecentJobIds.Count > 50) _state.State.RecentJobIds.RemoveAt(0);
                _state.State.LastRunAt = DateTimeOffset.UtcNow;
                _state.State.RunCount++;

                await _state.WriteStateAsync();

                ScheduleNext(DateTimeOffset.UtcNow);
            }
            finally
            {
                _tickInProgress = false;
            }
        }

        private static JobSpec CloneJobSpec(JobSpec src)
        {
            return new JobSpec
            {
                Name = src.Name,
                CorrelationId = src.CorrelationId,
                TelemetryProviderKey = src.TelemetryProviderKey,
                FailFast = src.FailFast,
                MaxDegreeOfParallelism = src.MaxDegreeOfParallelism,
                Tasks = src.Tasks.ToDictionary(kv => kv.Key, kv => new TaskSpec
                {
                    TaskId = kv.Value.TaskId,
                    ExecutorType = kv.Value.ExecutorType,
                    CommandJson = kv.Value.CommandJson,
                    Dependencies = new System.Collections.Generic.List<string>(kv.Value.Dependencies),
                    Cron = kv.Value.Cron,
                    CronTimeZone = kv.Value.CronTimeZone,
                    AllowConcurrentRuns = kv.Value.AllowConcurrentRuns,
                    MaxRuns = kv.Value.MaxRuns,
                    NotBefore = kv.Value.NotBefore,
                    NotAfter = kv.Value.NotAfter
                })
            };
        }
    }
}
