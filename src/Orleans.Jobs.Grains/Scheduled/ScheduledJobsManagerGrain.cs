using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Scheduled
{
    public class ScheduledJobsManagerGrain : Grain, IScheduledJobsManagerGrain
    {
        private const string IndexId = "index";

        public async Task<string> CreateAsync(ScheduledJobSpec spec)
        {
            if (string.IsNullOrWhiteSpace(spec.TemplateId))
                spec.TemplateId = this.GetPrimaryKeyString() + ":" + System.Guid.NewGuid().ToString("N");

            var g = GrainFactory.GetGrain<IScheduledJobGrain>(spec.TemplateId);
            await g.ConfigureAsync(spec);

            await GrainFactory.GetGrain<IScheduledJobsIndexGrain>(IndexId).AddAsync(spec.TemplateId);
            return spec.TemplateId;
        }

        public Task UpdateAsync(ScheduledJobSpec spec) =>
            GrainFactory.GetGrain<IScheduledJobGrain>(spec.TemplateId).UpdateSpecAsync(spec);

        public Task<ScheduledJobState> GetAsync(string templateId) =>
            GrainFactory.GetGrain<IScheduledJobGrain>(templateId).GetStateAsync();

        public Task<List<string>> ListAsync() =>
            GrainFactory.GetGrain<IScheduledJobsIndexGrain>(IndexId).ListAsync();

        public async Task DeleteAsync(string templateId)
        {
            await GrainFactory.GetGrain<IScheduledJobGrain>(templateId).DeleteAsync();
            await GrainFactory.GetGrain<IScheduledJobsIndexGrain>(IndexId).RemoveAsync(templateId);
        }

        public Task EnableAsync(string templateId) => GrainFactory.GetGrain<IScheduledJobGrain>(templateId).EnableAsync();
        public Task DisableAsync(string templateId) => GrainFactory.GetGrain<IScheduledJobGrain>(templateId).DisableAsync();
        public Task PauseAsync(string templateId) => GrainFactory.GetGrain<IScheduledJobGrain>(templateId).PauseAsync();
        public Task ResumeAsync(string templateId) => GrainFactory.GetGrain<IScheduledJobGrain>(templateId).ResumeAsync();
        public Task RunNowAsync(string templateId) => GrainFactory.GetGrain<IScheduledJobGrain>(templateId).RunNowAsync();
    }
}
