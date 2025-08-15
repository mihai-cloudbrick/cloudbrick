using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Cloudbrick.Components.Jobs.Models;
using Cloudbrick.Components.Jobs.Options;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Microsoft.Extensions.Options;

namespace Cloudbrick.Components.Jobs.Services
{
    public class HttpJobsBackend : IJobsBackend
    {
        private readonly HttpClient _http;
        private readonly CloudbrickJobsOptions _opt;

        public HttpJobsBackend(HttpClient http, IOptions<CloudbrickJobsOptions> opt)
        {
            _http = http;
            _opt = opt.Value;
        }

        public async Task<List<JobSummary>> ListJobsAsync(CancellationToken ct = default)
        {
            var url = $"{_opt.ApiBaseUrl}";
            var res = await _http.GetFromJsonAsync<List<JobSummary>>(url, ct);
            return res ?? new List<JobSummary>();
        }

        public async Task<JobDetailModel> GetJobAsync(Guid id, CancellationToken ct = default)
        {
            var url = $"{_opt.ApiBaseUrl}/{id}";
            var res = await _http.GetFromJsonAsync<JobDetailModel>(url, ct);
            return res ?? new JobDetailModel { JobId = id };
        }

        public Task PauseJobAsync(Guid id, CancellationToken ct = default) =>
            _http.PostAsync($"{_opt.ApiBaseUrl}/{id}/pause", content: null, ct);

        public Task ResumeJobAsync(Guid id, CancellationToken ct = default) =>
            _http.PostAsync($"{_opt.ApiBaseUrl}/{id}/resume", content: null, ct);

        public Task CancelJobAsync(Guid id, CancellationToken ct = default) =>
            _http.PostAsync($"{_opt.ApiBaseUrl}/{id}/cancel", content: null, ct);

        public async Task<Guid> CreateJobAsync(object spec, CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync($"{_opt.ApiBaseUrl}", spec, ct);
            resp.EnsureSuccessStatusCode();
            var id = await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
            return id;
        }

        public Task StartJobAsync(Guid id, CancellationToken ct = default) =>
            _http.PostAsync($"{_opt.ApiBaseUrl}/{id}/start", content: null, ct);
    }
}
