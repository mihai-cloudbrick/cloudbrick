using Microsoft.AspNetCore.SignalR;
using Orleans;

namespace Cloudbrick.Orleans.SignalR;

public class DynamicHub(IClusterClient cluster, HubRouting routing, IHubSubscriptionCoordinator subs) : Hub
{
    string HubName => routing.Resolve(Context.GetHttpContext()!);
    IHubGrain HubGrain => cluster.GetGrain<IHubGrain>(HubName);

    public override async Task OnConnectedAsync()
    {
        await subs.EnsureSubscribed(HubName);

        var userId = Context.UserIdentifier;
        await Groups.AddToGroupAsync(Context.ConnectionId, routing.HubGroup(HubName));
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, routing.UserGroup(HubName, userId!));

        await HubGrain.RegisterConnection(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        await HubGrain.UnregisterConnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(ex);
    }

    public async Task Join(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, routing.Group(HubName, group));
        await HubGrain.JoinGroup(Context.ConnectionId, group);
    }

    public async Task Leave(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, routing.Group(HubName, group));
        await HubGrain.LeaveGroup(Context.ConnectionId, group);
    }
}
