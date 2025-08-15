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


        public virtual async Task<(IScheduledJobsManager Manager, string Id)> CreateAsync(ScheduledJobSpec spec)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            var id = await mgr.CreateAsync(spec);
            return (this, id);
        }

        public virtual async Task<IScheduledJobsManager> UpdateAsync(string id, ScheduledJobSpec spec)
        {
            spec.TemplateId = id;
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            await mgr.UpdateAsync(spec);
            return this;
        }

        public virtual async Task<IScheduledJobsManager> DeleteAsync(string id)
        {
            var mgr = _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler");
            await mgr.DeleteAsync(id);
            return this;
        }

        public virtual async Task<IScheduledJobsManager> EnableAsync(string id)
        {
            await _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").EnableAsync(id);
            return this;
        }

        public virtual async Task<IScheduledJobsManager> DisableAsync(string id)
        {
            await _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").DisableAsync(id);
            return this;
        }

        public virtual async Task<IScheduledJobsManager> PauseAsync(string id)
        {
            await _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").PauseAsync(id);
            return this;
        }

        public virtual async Task<IScheduledJobsManager> ResumeAsync(string id)
        {
            await _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").ResumeAsync(id);
            return this;
        }

        public virtual async Task<IScheduledJobsManager> RunNowAsync(string id)
        {
            await _orleans.GetGrain<IScheduledJobsManagerGrain>("scheduler").RunNowAsync(id);
            return this;
        }
    }
}
