using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Telemetry
{
    public class TelemetryHub : Hub
    {
        private readonly ILogger<TelemetryHub> _logger;
        private readonly TelemetrySyncService _sync;

        public TelemetryHub(ILogger<TelemetryHub> logger, TelemetrySyncService sync)
        {
            _logger = logger;
            _sync = sync;
        }

        public async Task SubscribeToJob(Guid jobId)
        {
            var group = GroupName(jobId);
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            await _sync.AddSubscriberAsync(jobId, Context.ConnectionId);
            _logger.LogInformation("Conn {conn}: subscribed to {job}", Context.ConnectionId, jobId);
        }

        public async Task UnsubscribeFromJob(Guid jobId)
        {
            var group = GroupName(jobId);
            await _sync.RemoveSubscriberAsync(jobId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            _logger.LogInformation("Conn {conn}: unsubscribed from {job}", Context.ConnectionId, jobId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await _sync.RemoveConnectionAsync(Context.ConnectionId); // cleans up all job refs
            await base.OnDisconnectedAsync(exception);
        }

        internal static string GroupName(Guid jobId) => $"job-{jobId:N}";
    }
}
