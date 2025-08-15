using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces
{
    public interface IScheduledJobsIndexGrain : IGrainWithStringKey
    {
        Task AddAsync(string id);
        Task RemoveAsync(string id);
        Task<List<string>> ListAsync();
    }
}
