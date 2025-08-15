using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Cloudbrick.JobApi.Controllers
{
    [ApiController]
    [Route("api/schedules")]
    public class ScheduledJobsController : ControllerBase
    {
        private readonly IClusterClient _orleans;
        private readonly ILogger<ScheduledJobsController> _logger;

        public ScheduledJobsController(IClusterClient orleans, ILogger<ScheduledJobsController> logger)
        {
            _orleans = orleans;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> ListAsync()
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var ids = await mgr.ListAsync();
            return Ok(ids);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduledJobState>> GetAsync(string id)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var st = await mgr.GetAsync(id);
            if (st == null) return NotFound();
            return Ok(st);
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateAsync([FromBody] ScheduledJobSpec spec)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var id = await mgr.CreateAsync(spec);
            return Ok(id);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] ScheduledJobSpec spec)
        {
            spec.TemplateId = id;
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            await mgr.UpdateAsync(spec);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            await mgr.DeleteAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/enable")]
        public Task EnableAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").EnableAsync(id);

        [HttpPost("{id}/disable")]
        public Task DisableAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").DisableAsync(id);

        [HttpPost("{id}/pause")]
        public Task PauseAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").PauseAsync(id);

        [HttpPost("{id}/resume")]
        public Task ResumeAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").ResumeAsync(id);

        [HttpPost("{id}/run")]
        public Task RunNowAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").RunNowAsync(id);
    }
}
