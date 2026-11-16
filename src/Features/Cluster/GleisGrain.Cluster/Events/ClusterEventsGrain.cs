using GleisGrain.Cluster.Contracts;
using Microsoft.Extensions.Options;
using Orleans.Utilities;

namespace GleisGrain.Cluster.Events;

/// <summary>
/// The hub. Keeps no state of its own: the observers are one per silo and renew themselves, so if the silo
/// hosting the hub dies, the next renewal subscribes them again on the silo that activates the hub next.
/// </summary>
internal sealed class ClusterEventsGrain : Grain, IClusterEventsGrain
{
    private readonly ObserverManager<ILiveEventObserver> _observers;
    private readonly ClusterMetrics _metrics;

    public ClusterEventsGrain(IOptions<ClusterOptions> options, TimeProvider time, ILogger<ClusterEventsGrain> logger, ClusterMetrics metrics)
    {
        _metrics = metrics;
        _observers = new ObserverManager<ILiveEventObserver>(options.Value.SubscriptionTtl, logger)
        {
            GetDateTime = () => time.GetUtcNow().UtcDateTime,
        };
    }

    public async Task PublishAsync(LiveEvent liveEvent)
    {
        _metrics.EventPublished(liveEvent.Type);
        _observers.ClearExpired();

        // The observer manager drops an observer whose call throws, so a dead silo never reaches the publisher.
        await _observers.Notify(observer => observer.OnEvent(liveEvent));
    }

    public Task SubscribeAsync(ILiveEventObserver observer)
    {
        _observers.Subscribe(observer, observer);

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(ILiveEventObserver observer)
    {
        _observers.Unsubscribe(observer);

        return Task.CompletedTask;
    }
}
