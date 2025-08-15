using Orleans;

namespace Cloudbrick.Orleans.SignalR;

public interface IHubMessageSender
{
    Task SendAsync(string hub, HubTargetKind kind, string? target, string clientMethod, params object?[] args);
    Task ToAll(string hub, string clientMethod, params object?[] args)
        => SendAsync(hub, HubTargetKind.All, null, clientMethod, args);
    Task ToUser(string hub, string userId, string clientMethod, params object?[] args)
        => SendAsync(hub, HubTargetKind.User, userId, clientMethod, args);
    Task ToGroup(string hub, string group, string clientMethod, params object?[] args)
        => SendAsync(hub, HubTargetKind.Group, group, clientMethod, args);
    Task ToConnection(string hub, string connectionId, string clientMethod, params object?[] args)
        => SendAsync(hub, HubTargetKind.Connection, connectionId, clientMethod, args);
}

public sealed class HubMessageSender(IClusterClient cluster) : IHubMessageSender
{
    public Task SendAsync(string hub, HubTargetKind kind, string? target, string clientMethod, params object?[] args)
        => cluster.GetGrain<IHubPublisherGrain>(hub)
                  .Publish(new HubEnvelope
                  {
                      Hub = hub,
                      TargetKind = kind,
                      Target = target,
                      Method = clientMethod,
                      Args = args
                  });
}
