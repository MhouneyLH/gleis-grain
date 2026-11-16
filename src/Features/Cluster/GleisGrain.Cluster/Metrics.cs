using System.Diagnostics.Metrics;

namespace GleisGrain.Cluster;

internal sealed class ClusterMetrics
{
    public const string MeterName = "GleisGrain.Cluster";

    private readonly Counter<long> _published;
    private readonly Counter<long> _delivered;
    private readonly UpDownCounter<int> _clients;

    public ClusterMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _published = meter.CreateCounter<long>(
            "gleisgrain.cluster.events.published",
            unit: "{event}",
            description: "Live events published to the hub, tagged by event type.");
        _delivered = meter.CreateCounter<long>(
            "gleisgrain.cluster.events.delivered",
            unit: "{event}",
            description: "Live events queued for a connected browser, tagged by event type.");
        _clients = meter.CreateUpDownCounter<int>(
            "gleisgrain.cluster.clients.connected",
            unit: "{client}",
            description: "Browsers connected to the event stream of this silo.");
    }

    public void EventPublished(string type) =>
        _published.Add(1, new KeyValuePair<string, object?>("type", type));

    public void EventDelivered(string type) =>
        _delivered.Add(1, new KeyValuePair<string, object?>("type", type));

    public void ClientConnected() => _clients.Add(1);

    public void ClientDisconnected() => _clients.Add(-1);
}
