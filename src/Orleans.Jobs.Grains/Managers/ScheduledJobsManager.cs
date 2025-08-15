using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Managers;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Managers
{
    internal class ScheduledJobsManager : IScheduledJobsManager
    {
        private readonly IClusterClient _orleans;
        private readonly ILogger<ScheduledJobsManager> _logger;

        public ScheduledJobsManager(IClusterClient orleans, ILogger<ScheduledJobsManager> logger)
        {
            _orleans = orleans;
            _logger = logger;
        }


        public virtual async Task<List<string>> ListAsync()
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var ids = await mgr.ListAsync();
            return ids;
        }


        public virtual async Task<ScheduledJobState?> GetAsync(string id)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var st = await mgr.GetAsync(id);
            if (st == null) return null;
            return st;
        }


        public virtual async Task<string> CreateAsync(ScheduledJobSpec spec)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var id = await mgr.CreateAsync(spec);
            return id;
        }

        public virtual async Task UpdateAsync(string id, ScheduledJobSpec spec)
        {
            spec.TemplateId = id;
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            await mgr.UpdateAsync(spec);
        }

        public virtual async Task DeleteAsync(string id)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            await mgr.DeleteAsync(id);
        }

        public virtual Task EnableAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").EnableAsync(id);

        public virtual Task DisableAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").DisableAsync(id);

        public virtual Task PauseAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").PauseAsync(id);

        public virtual Task ResumeAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").ResumeAsync(id);

        public virtual Task RunNowAsync(string id) => _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").RunNowAsync(id);
    }
}
