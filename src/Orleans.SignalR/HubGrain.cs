using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.SignalR;

[GenerateSerializer]
public sealed class HubState
{
    [Id(0)] public HashSet<string> Connections { get; set; } = new();
    [Id(1)] public Dictionary<string, HashSet<string>> Groups { get; set; } = new(); // group->connIds
    [Id(2)] public Dictionary<string, HashSet<string>> Users  { get; set; } = new(); // userId->connIds
}

public sealed class HubGrain([PersistentState("state")] IPersistentState<HubState> state)
    : Grain, IHubGrain
{
    private readonly IPersistentState<HubState> _state = state;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await GrainFactory.GetGrain<IHubDirectoryGrain>("singleton").AddHubUsage(this.GetPrimaryKeyString());
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct)
    {
        if (_state.State.Connections.Count == 0)
        {
            await GrainFactory.GetGrain<IHubDirectoryGrain>("singleton").RemoveHubUsage(this.GetPrimaryKeyString());
        }
        await base.OnDeactivateAsync(reason, ct);
    }

    public Task RegisterConnection(string connectionId, string? userId)
    {
        _state.State.Connections.Add(connectionId);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (!_state.State.Users.TryGetValue(userId!, out var set))
                _state.State.Users[userId!] = set = new();
            set.Add(connectionId);
        }
        return _state.WriteStateAsync();
    }

    public Task UnregisterConnection(string connectionId)
    {
        _state.State.Connections.Remove(connectionId);

        foreach (var kv in _state.State.Groups.ToList())
        {
            kv.Value.Remove(connectionId);
            if (kv.Value.Count == 0) _state.State.Groups.Remove(kv.Key);
        }

        foreach (var kv in _state.State.Users.ToList())
        {
            kv.Value.Remove(connectionId);
            if (kv.Value.Count == 0) _state.State.Users.Remove(kv.Key);
        }

        return _state.WriteStateAsync();
    }

    public Task JoinGroup(string connectionId, string group)
    {
        if (!_state.State.Groups.TryGetValue(group, out var set))
            _state.State.Groups[group] = set = new();
        set.Add(connectionId);
        return _state.WriteStateAsync();
    }

    public Task LeaveGroup(string connectionId, string group)
    {
        if (_state.State.Groups.TryGetValue(group, out var set))
        {
            set.Remove(connectionId);
            if (set.Count == 0) _state.State.Groups.Remove(group);
        }
        return _state.WriteStateAsync();
    }
}
