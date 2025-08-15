using Orleans;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.SignalR;

public enum HubTargetKind { All, Group, User, Connection }

[GenerateSerializer]
public sealed class HubEnvelope
{
    [Id(0)] public string Hub { get; init; } = default!;       // logical hub name
    [Id(1)] public HubTargetKind TargetKind { get; init; }     // All | Group | User | Connection
    [Id(2)] public string? Target { get; init; }               // group/user/connection id
    [Id(3)] public string Method { get; init; } = default!;    // client method
    [Id(4)] public object?[] Args { get; init; } = Array.Empty<object?>();

    // optional metadata
    [Id(5)] public string MessageId { get; init; } = Guid.NewGuid().ToString("N");
    [Id(6)] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    [Id(7)] public string? CorrelationId { get; init; }
    [Id(8)] public string? PartitionKey { get; init; }
}

// Tracks membership for a hub (connections/users/groups).
public interface IHubGrain : IGrainWithStringKey
{
    Task RegisterConnection(string connectionId, string? userId);
    Task UnregisterConnection(string connectionId);
    Task JoinGroup(string connectionId, string group);
    Task LeaveGroup(string connectionId, string group);
}

// Publishes messages for a hub (one-at-a-time due to grain single-threading).
public interface IHubPublisherGrain : IGrainWithStringKey
{
    Task Publish(HubEnvelope message);
}

// Optional directory listing hubs in-use (for pre-subscription, ops).
public interface IHubDirectoryGrain : IGrainWithStringKey
{
    Task AddHubUsage(string hub);
    Task RemoveHubUsage(string hub);
    Task<IReadOnlyCollection<string>> ListHubs();
}
