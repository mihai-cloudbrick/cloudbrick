using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;

namespace Cloudbrick.Orleans.SignalR;

internal sealed class Deduper
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _seen = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
    private readonly int _max = 50_000;

    public bool ShouldProcess(string id, DateTimeOffset tsUtc)
    {
        CleanupIfNeeded();
        var now = DateTimeOffset.UtcNow;
        if (tsUtc + _ttl < now) return false;
        return _seen.TryAdd(id, now);
    }

    private void CleanupIfNeeded()
    {
        if (_seen.Count < _max) return;
        var cutoff = DateTimeOffset.UtcNow - _ttl;
        foreach (var kv in _seen)
            if (kv.Value < cutoff) _seen.TryRemove(kv.Key, out _);
    }
}

public sealed class HubRelayHostedService(
    IClusterClient cluster,
    IHubContext<DynamicHub> hubContext,
    HubRouting routing,
    ILogger<HubRelayHostedService> logger)
    : BackgroundService, IHubSubscriptionCoordinator
{
    private readonly ConcurrentDictionary<string, StreamSubscriptionHandle<HubEnvelope>> _subs = new();
    private readonly ConcurrentDictionary<string, Deduper> _dedup = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Optional: subscribe known hubs at startup
        try
        {
            var known = await cluster.GetGrain<IHubDirectoryGrain>("singleton").ListHubs();
            foreach (var hub in known) await EnsureSubscribed(hub);
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Directory grain not yet populated; subscriptions will be lazy.");
        }
    }

    public async Task EnsureSubscribed(string hub)
    {
        if (_subs.ContainsKey(hub)) return;

        var provider = cluster.GetStreamProvider("HubStream");
        var stream = provider.GetStream<HubEnvelope>(StreamId.Create("HubTopic", hub));

        var ded = _dedup.GetOrAdd(hub, _ => new Deduper());

        async Task OnNext(HubEnvelope e, StreamSequenceToken? token)
        {
            if (!ded.ShouldProcess(e.MessageId, e.Timestamp)) return;

            try
            {
                switch (e.TargetKind)
                {
                    case HubTargetKind.All:
                        await hubContext.Clients.Group(routing.HubGroup(e.Hub)).SendAsync(e.Method, e.Args);
                        break;
                    case HubTargetKind.Group:
                        await hubContext.Clients.Group(routing.Group(e.Hub, e.Target!)).SendAsync(e.Method, e.Args);
                        break;
                    case HubTargetKind.User:
                        await hubContext.Clients.Group(routing.UserGroup(e.Hub, e.Target!)).SendAsync(e.Method, e.Args);
                        break;
                    case HubTargetKind.Connection:
                        await hubContext.Clients.Client(e.Target!).SendAsync(e.Method, e.Args);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Delivery failed for hub {Hub} method {Method}", e.Hub, e.Method);
            }
        }

        async Task OnError(Exception ex)
        {
            logger.LogError(ex, "Stream error for hub {Hub}; resubscribing...", hub);
            _subs.TryRemove(hub, out _);
            await EnsureSubscribed(hub);
        }

        var handle = await stream.SubscribeAsync(OnNext, OnError, async () =>
        {
            logger.LogInformation("Stream completed for hub {Hub}", hub);
            _subs.TryRemove(hub, out _);
        });

        _subs[hub] = handle;
    }
}
