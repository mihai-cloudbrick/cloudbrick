using Cloudbrick.Orleans.Jobs.Abstractions;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Telemetry
{
    public class TelemetrySyncService : IAsyncDisposable
    {
        private readonly IClusterClient _client;
        private readonly IHubContext<TelemetryHub> _hub;
        private readonly ILogger<TelemetrySyncService> _logger;

        private class JobSub
        {
            public int RefCount;
            public StreamSubscriptionHandle<ExecutionEvent>? Handle;
            public HashSet<string> Connections = new HashSet<string>(StringComparer.Ordinal);
        }

        private readonly ConcurrentDictionary<Guid, JobSub> _jobs = new();
        private readonly ConcurrentDictionary<string, HashSet<Guid>> _byConnection = new(); // connId -> jobIds

        public TelemetrySyncService(IClusterClient client,
                                    IHubContext<TelemetryHub> hub,
                                    ILogger<TelemetrySyncService> logger)
        {
            _client = client;
            _hub = hub;
            _logger = logger;
        }

        public async Task AddSubscriberAsync(Guid jobId, string connectionId)
        {
            var sub = _jobs.GetOrAdd(jobId, _ => new JobSub());
            lock (sub)
            {
                sub.RefCount++;
                sub.Connections.Add(connectionId);
            }

            _byConnection.AddOrUpdate(connectionId,
                addValueFactory: _ => new HashSet<Guid> { jobId },
                updateValueFactory: (_, set) => { set.Add(jobId); return set; });

            if (sub.Handle == null)
            {
                // create Orleans stream subscription (first subscriber)
                var provider = _client.GetStreamProvider(StreamConstants.ProviderName);
                var stream = provider.GetStream<ExecutionEvent>(StreamId.Create(StreamConstants.JobNamespace, jobId.ToString()));
                sub.Handle = await stream.SubscribeAsync(async (evt, ct) =>
                {
                    // broadcast to SignalR group
                    await _hub.Clients.Group(TelemetryHub.GroupName(jobId)).SendAsync("telemetry", evt);
                },
                async ex =>
                {
                    _logger.LogWarning(ex, "Job stream {job} faulted; attempting resubscribe", jobId);
                    lock (sub) sub.Handle = null;
                    // resubscribe on demand (next AddSubscriber or tick)
                },
                async () => { /* stream complete */ });
            }
        }

        public async Task RemoveSubscriberAsync(Guid jobId, string connectionId)
        {
            if (!_jobs.TryGetValue(jobId, out var sub)) return;

            lock (sub)
            {
                if (sub.Connections.Remove(connectionId))
                    sub.RefCount = Math.Max(0, sub.RefCount - 1);
            }

            if (_byConnection.TryGetValue(connectionId, out var set))
            {
                lock (set) set.Remove(jobId);
            }

            if (sub.RefCount == 0)
            {
                // last subscriber → detach Orleans handle
                var handle = sub.Handle;
                sub.Handle = null;
                _jobs.TryRemove(jobId, out _);
                if (handle != null)
                {
                    try { await handle.UnsubscribeAsync(); } catch { /* ignore */ }
                }
            }
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            if (_byConnection.TryRemove(connectionId, out var jobs))
            {
                foreach (var jobId in jobs)
                    await RemoveSubscriberAsync(jobId, connectionId);
            }
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kv in _jobs)
            {
                try { if (kv.Value.Handle != null) await kv.Value.Handle.UnsubscribeAsync(); }
                catch { /* ignore */ }
            }
            _jobs.Clear();
        }
    }
}
