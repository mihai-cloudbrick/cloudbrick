using Microsoft.AspNetCore.Mvc;
using Orleans;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.JobApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly IClusterClient _orleans;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IClusterClient orleans, ILogger<JobsController> logger)
        {
            _orleans = orleans;
            _logger = logger;
        }

        // GET /api/jobs
        [HttpGet]
        public async Task<ActionResult<List<JobSummaryDto>>> ListAsync()
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var ids = await mgr.ListJobsAsync();
            var list = new List<JobSummaryDto>(ids.Count);
            foreach (var id in ids)
            {
                var st = await mgr.GetJobStateAsync(id);
                if (st != null)
                    list.Add(ApiModelMapper.ToSummary(st));
            }
            return Ok(list);
        }

        // GET /api/jobs/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<JobDetailDto>> GetAsync(Guid id)
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var st = await mgr.GetJobStateAsync(id);
            if (st == null) return NotFound();
            return Ok(ApiModelMapper.ToDetail(st));
        }

        // POST /api/jobs  (body: JobSpec)
        [HttpPost]
        public async Task<ActionResult<Guid>> CreateAsync([FromBody] JobSpec spec)
        {
            if (spec == null) return BadRequest("Missing job spec");
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var id = await mgr.CreateJobAsync(spec);
            return Ok(id);
        }

        // POST /api/jobs/{id}/start
        [HttpPost("{id:guid}/start")]
        public async Task<IActionResult> StartAsync(Guid id)
        {
            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            await mgr.StartJobAsync(id);
            return NoContent();
        }

        // POST /api/jobs/{id}/pause
        [HttpPost("{id:guid}/pause")]
        public async Task<IActionResult> PauseAsync(Guid id)
        {
            await _orleans.GetGrain<IJobGrain>(id).PauseAsync();
            return NoContent();
        }

        // POST /api/jobs/{id}/resume
        [HttpPost("{id:guid}/resume")]
        public async Task<IActionResult> ResumeAsync(Guid id)
        {
            await _orleans.GetGrain<IJobGrain>(id).ResumeAsync();
            return NoContent();
        }

        // POST /api/jobs/{id}/cancel
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> CancelAsync(Guid id)
        {
            await _orleans.GetGrain<IJobGrain>(id).CancelAsync();
            return NoContent();
        }
    }

    internal static class ApiModelMapper
    {
        public static JobSummaryDto ToSummary(JobState s) => new JobSummaryDto
        {
            JobId = s.JobId,
            Status = (int)s.Status,
            CorrelationId = s.CorrelationId,
            StartedAt = s.StartedAt,
            JobProgress = s.JobProgress
        };

        public static JobDetailDto ToDetail(JobState s)
        {
            var dto = new JobDetailDto
            {
                JobId = s.JobId,
                Status = (int)s.Status,
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
                dto.Tasks[kv.Key] = new TaskSummaryDto
                {
                    TaskId = t.TaskId,
                    ExecutorType = t.ExecutorType,
                    Status = (int)t.Status,
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

    #region DTOs returned by the API (match Cloudbrick.Components.Jobs Models shape)

    public class JobSummaryDto
    {
        public Guid JobId { get; set; }
        public int Status { get; set; } // enum numeric
        public string? CorrelationId { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public int JobProgress { get; set; }
    }

    public class TaskSummaryDto
    {
        public string TaskId { get; set; } = string.Empty;
        public string ExecutorType { get; set; } = string.Empty;
        public int Status { get; set; } // enum numeric
        public int Progress { get; set; }
        public string? LastError { get; set; }
        public int RunCount { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
    }

    public class JobDetailDto
    {
        public Guid JobId { get; set; }
        public int Status { get; set; } // enum numeric
        public string? CorrelationId { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }

        public Dictionary<string, TaskSummaryDto> Tasks { get; set; } = new();

        public int TotalTasks { get; set; }
        public int RunningTasks { get; set; }
        public int QueuedTasks { get; set; }
        public int SucceededTasks { get; set; }
        public int FailedTasks { get; set; }
        public int CancelledTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int JobProgress { get; set; }
    }

    #endregion
}
