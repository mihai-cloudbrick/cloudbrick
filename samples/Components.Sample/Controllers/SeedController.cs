using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Microsoft.AspNetCore.Mvc;

namespace Components.Sample.Controllers
{
    [ApiController]
    [Route("api/seed")]
    public class SeedController : ControllerBase
    {
        private readonly IClusterClient _orleans;
        private readonly ILogger<SeedController> _logger;

        public SeedController(IClusterClient orleans, ILogger<SeedController> logger)
        {
            _orleans = orleans;
            _logger = logger;
        }

        /// <summary>
        /// Creates and starts a simple job with "adder" then "delay" (adder -> wait).
        /// </summary>
        [HttpPost("job/basic")]
        public async Task<ActionResult<Guid>> CreateBasicJobAsync()
        {
            var spec = new JobSpec
            {
                Name = "basic-demo",
                CorrelationId = Guid.NewGuid().ToString("N"),
                TelemetryProviderKey = "console",
                MaxDegreeOfParallelism = 1,
                FailFast = true
            };

            spec.Tasks["adder"] = new TaskSpec { ExecutorType = "adder", CommandJson = "{ \"a\": 40, \"b\": 2 }" };
            spec.Tasks["wait"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1000, \"steps\": 20 }", Dependencies = { "adder" } };

            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var jobId = await mgr.CreateJobAsync(spec);
            await mgr.StartJobAsync(jobId);

            _logger.LogInformation("Seeded basic job {JobId}", jobId);
            return Ok(jobId);
        }

        /// <summary>
        /// Creates and starts a job where "tick" runs every 10 seconds (CRON at task level).
        /// </summary>
        [HttpPost("job/cron-task")]
        public async Task<ActionResult<Guid>> CreateCronTaskJobAsync()
        {
            var spec = new JobSpec
            {
                Name = "cron-task-demo",
                CorrelationId = Guid.NewGuid().ToString("N"),
                TelemetryProviderKey = "console",
                MaxDegreeOfParallelism = 1,
                FailFast = false
            };

            spec.Tasks["tick"] = new TaskSpec
            {
                ExecutorType = "delay",
                CommandJson = "{ \"milliseconds\": 1000, \"steps\": 5 }",
                Cron = "*/10 * * * * *", // every 10 seconds (with seconds field)
                CronTimeZone = "UTC",
                AllowConcurrentRuns = false
            };

            var mgr = _orleans.GetGrain<IJobsManagerGrain>("manager");
            var jobId = await mgr.CreateJobAsync(spec);
            await mgr.StartJobAsync(jobId);

            _logger.LogInformation("Seeded cron task job {JobId}", jobId);
            return Ok(jobId);
        }

        /// <summary>
        /// Creates a Scheduled Job template that runs nightly at 02:00 Europe/Bucharest.
        /// </summary>
        [HttpPost("schedule/nightly")]
        public async Task<ActionResult<string>> CreateNightlyScheduleAsync()
        {
            var job = new JobSpec
            {
                Name = "nightly-etl",
                CorrelationId = Guid.NewGuid().ToString("N"),
                TelemetryProviderKey = "console",
                MaxDegreeOfParallelism = 2,
                FailFast = true
            };

            job.Tasks["extract"] = new TaskSpec { ExecutorType = "adder", CommandJson = "{ \"a\": 1, \"b\": 2 }" };
            job.Tasks["wait"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1000, \"steps\": 10 }", Dependencies = { "extract" } };

            var spec = new ScheduledJobSpec
            {
                TemplateId = "nightly-etl",
                Job = job,
                Cron = "0 2 * * *",
                CronTimeZone = "Europe/Bucharest",
                AllowOverlappingJobs = false
            };

            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var id = await mgr.CreateAsync(spec);

            return Ok(id);
        }
    }
}
