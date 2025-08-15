using Orleans;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.SignalR;

[GenerateSerializer]
public sealed class DirectoryState { [Id(0)] public HashSet<string> Hubs { get; set; } = new(); }

public sealed class HubDirectoryGrain([PersistentState("state")] IPersistentState<DirectoryState> st)
    : Grain, IHubDirectoryGrain
{
    public Task AddHubUsage(string hub) { st.State.Hubs.Add(hub); return st.WriteStateAsync(); }
    public Task RemoveHubUsage(string hub) { st.State.Hubs.Remove(hub); return st.WriteStateAsync(); }
    public Task<IReadOnlyCollection<string>> ListHubs()
        => Task.FromResult((IReadOnlyCollection<string>)st.State.Hubs.ToArray());
}
