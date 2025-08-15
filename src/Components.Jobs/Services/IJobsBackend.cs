using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cloudbrick.Components.Jobs.Models;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Components.Jobs.Services
{
    public interface IJobsBackend
    {
        Task<List<JobSummary>> ListJobsAsync(CancellationToken ct = default);
        Task<JobDetailModel> GetJobAsync(Guid id, CancellationToken ct = default);
        Task PauseJobAsync(Guid id, CancellationToken ct = default);
        Task ResumeJobAsync(Guid id, CancellationToken ct = default);
        Task CancelJobAsync(Guid id, CancellationToken ct = default);
        Task<Guid> CreateJobAsync(object spec, CancellationToken ct = default);
        Task StartJobAsync(Guid id, CancellationToken ct = default);
    }
}
