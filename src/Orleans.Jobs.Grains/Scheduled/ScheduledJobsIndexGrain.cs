using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Scheduled
{
    public class ScheduledJobsIndexGrain : Grain, IScheduledJobsIndexGrain
    {
        private readonly IPersistentState<IndexState> _state;

        public class IndexState
        {
            public List<string> Ids { get; set; } = new List<string>();
        }

        public ScheduledJobsIndexGrain([PersistentState("schedIndex", "Default")] IPersistentState<IndexState> state)
        {
            _state = state;
        }

        public async Task AddAsync(string id)
        {
            if (!_state.State.Ids.Contains(id))
            {
                _state.State.Ids.Add(id);
                await _state.WriteStateAsync();
            }
        }

        public async Task RemoveAsync(string id)
        {
            if (_state.State.Ids.Remove(id))
                await _state.WriteStateAsync();
        }

        public Task<List<string>> ListAsync() => Task.FromResult(_state.State.Ids.ToList());
    }
}
