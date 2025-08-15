using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Managers;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.Orleans.Jobs.Managers
{
    internal class JobsManager : IJobsManager
    {
        private readonly IClusterClient _orleans;
        private readonly ILogger<JobsManager> _logger;

        public JobsManager(IClusterClient orleans, ILogger<JobsManager> logger)
        {
            _orleans = orleans;
            _logger = logger;
        }
        public virtual async Task<List<JobSummary>> ListAsync()
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var ids = await mgr.ListJobsAsync();
            var list = new List<JobSummary>(ids.Count);
            foreach (var id in ids)
            {
                var st = await mgr.GetJobStateAsync(id);
                list.Add(ApiModelMapper.ToSummary(st));
            }
            return list;
        }
        public virtual async Task<JobDetail?> GetAsync(Guid id)
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var st = await mgr.GetJobStateAsync(id);
            if (st == null) return null;
            return ApiModelMapper.ToDetail(st);
        }
        public virtual async Task<Guid> CreateAsync(JobSpec spec)
        {
            if (spec == null) throw new Exception("Missing job spec");
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var id = await mgr.CreateJobAsync(spec);
            return id;
        }
        public virtual async Task<IJobsManager> StartAsync(Guid id)
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            await mgr.StartJobAsync(id);
            return this;
        }
        public virtual async Task<IJobsManager> PauseAsync(Guid id)
        {
            await _orleans.GetGrain<IJobGrain>(id).PauseAsync();
            return this;
        }
        public virtual async Task<IJobsManager> ResumeAsync(Guid id)
        {
            await _orleans.GetGrain<IJobGrain>(id).ResumeAsync();
            return this;
        }
        public virtual async Task<IJobsManager> CancelAsync(Guid id)
        {
            await _orleans.GetGrain<IJobGrain>(id).CancelAsync();
            return this;
        }
        public virtual async Task<IJobsManager> DeleteAsync(Guid id)
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            await mgr.DeleteJobAsync(id);
            return this;
        }

        internal static class ApiModelMapper
        {
            public static JobSummary ToSummary(JobState s) => new JobSummary
            {
                JobId = s.JobId,
                Status = s.Status,
                CorrelationId = s.CorrelationId,
                StartedAt = s.StartedAt,
                JobProgress = s.JobProgress
            };

            public static JobDetail ToDetail(JobState s)
            {
                var dto = new JobDetail
                {
                    JobId = s.JobId,
                    Status = s.Status,
                    CorrelationId = s.CorrelationId,
                    StartedAt = s.StartedAt,
                    CompletedAt = s.CompletedAt,
                    TotalTasks = s.TotalTasks,
                    RunningTasks = s.RunningTasks,
                    QueuedTasks = s.QueuedTasks,
                    SucceededTasks = s.SucceededTasks,
                    FailedTasks = s.FailedTasks,
                    CancelledTasks = s.CancelledTasks,
                    CompletedTasks = s.CompletedTasks,
                    JobProgress = s.JobProgress
                };

                foreach (var kv in s.Tasks)
                {
                    var t = kv.Value;
                    dto.Tasks[kv.Key] = new TaskSummary
                    {
                        TaskId = t.TaskId,
                        ExecutorType = t.ExecutorType,
                        Status = t.Status,
                        Progress = t.Progress,
                        LastError = t.LastError,
                        RunCount = t.RunCount,
                        NextRunAt = t.NextRunAt,
                        CompletedAt = t.CompletedAt
                    };
                }

                return dto;
            }
        }
    }
}
