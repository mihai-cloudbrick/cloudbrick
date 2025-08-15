using Orleans;
using Orleans.Streams;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.SignalR;

// Per hub-key publisher; Orleans serializes execution so each hub publishes one message at a time.
public sealed class HubPublisherGrain : Grain, IHubPublisherGrain
{
    private const string Provider = "HubStream";
    private const string Namespace = "HubTopic";

    public Task Publish(HubEnvelope m)
    {
        var hub = this.GetPrimaryKeyString();
        var sp = this.GetStreamProvider(Provider);
        var stream = sp.GetStream<HubEnvelope>(StreamId.Create(Namespace, hub));
        return stream.OnNextAsync(m);
    }
}
