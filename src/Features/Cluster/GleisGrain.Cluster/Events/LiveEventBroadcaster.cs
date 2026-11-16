using System.Collections.Concurrent;
using System.Threading.Channels;
using GleisGrain.Cluster.Contracts;

namespace GleisGrain.Cluster.Events;

/// <summary>
/// Fans the events this silo receives out to the browsers connected to this silo, one bounded queue per browser.
/// A slow browser loses its oldest events instead of holding anybody else up.
/// </summary>
internal sealed class LiveEventBroadcaster(ClusterMetrics metrics)
{
    public const int ClientCapacity = 64;

    private readonly ConcurrentDictionary<Channel<LiveEvent>, byte> _clients = new();

    /// <summary>Adds a browser. Dispose the result to remove it again.</summary>
    public LiveEventSubscription Subscribe()
    {
        var channel = Channel.CreateBounded<LiveEvent>(new BoundedChannelOptions(ClientCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

        _clients[channel] = 0;
        metrics.ClientConnected();

        return new LiveEventSubscription(channel.Reader, () => Remove(channel));
    }

    public void Publish(LiveEvent liveEvent)
    {
        foreach (var channel in _clients.Keys)
        {
            // Never blocks: with DropOldest a full queue makes room itself. Fails only for a client that was just removed.
            if (channel.Writer.TryWrite(liveEvent))
            {
                metrics.EventDelivered(liveEvent.Type);
            }
        }
    }

    private void Remove(Channel<LiveEvent> channel)
    {
        if (_clients.TryRemove(channel, out _))
        {
            channel.Writer.TryComplete();
            metrics.ClientDisconnected();
        }
    }
}

/// <summary>The events queued for one connected browser.</summary>
internal sealed class LiveEventSubscription(ChannelReader<LiveEvent> reader, Action remove) : IDisposable
{
    public ChannelReader<LiveEvent> Reader { get; } = reader;

    public void Dispose() => remove();
}
