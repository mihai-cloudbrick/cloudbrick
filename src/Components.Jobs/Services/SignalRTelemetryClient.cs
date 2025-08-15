using System;
using System.Threading.Tasks;
using Cloudbrick.Components.Jobs.Models;
using Cloudbrick.Components.Jobs.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cloudbrick.Components.Jobs.Services
{
    public class SignalRTelemetryClient : IAsyncDisposable
    {
        private readonly NavigationManager _nav;
        private readonly ILogger<SignalRTelemetryClient> _log;
        private readonly CloudbrickJobsOptions _opt;
        private HubConnection? _hub;

        public SignalRTelemetryClient(NavigationManager nav, IOptions<CloudbrickJobsOptions> opt, ILogger<SignalRTelemetryClient> log)
        {
            _nav = nav;
            _log = log;
            _opt = opt.Value;
        }

        public async Task StartAsync()
        {
            if (_hub != null) return;
            var url = _nav.ToAbsoluteUri(_opt.TelemetryHubUrl);
            _hub = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();
            await _hub.StartAsync();
        }

        public async Task SubscribeAsync(Guid jobId, Action<ExecutionEventModel> onEvent)
        {
            if (_hub == null) await StartAsync();
            _hub!.On<ExecutionEventModel>("telemetry", evt =>
            {
                if (evt.JobId == jobId) onEvent(evt);
            });
            await _hub!.InvokeAsync("SubscribeToJob", jobId);
        }

        public async Task UnsubscribeAsync(Guid jobId)
        {
            if (_hub != null)
            {
                try { await _hub.InvokeAsync("UnsubscribeFromJob", jobId); } catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hub != null)
            {
                try { await _hub.StopAsync(); } catch { }
                await _hub.DisposeAsync();
                _hub = null;
            }
        }
    }
}
