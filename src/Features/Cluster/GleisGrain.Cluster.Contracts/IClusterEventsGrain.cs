using Orleans.Concurrency;

namespace GleisGrain.Cluster.Contracts;

/// <summary>
/// The hub every module publishes live events to. Each silo subscribes one observer, so an event
/// raised on any silo reaches the browsers connected to every silo.
/// </summary>
/// <remarks>The grain key is <see cref="ClusterEventKeys.Hub"/>.</remarks>
[Alias("GleisGrain.Cluster.Contracts.IClusterEventsGrain")]
public interface IClusterEventsGrain : IGrainWithStringKey
{
    /// <summary>Fire and forget: a lost event is replaced by the next one, no caller waits for the browsers.</summary>
    [OneWay, Alias("PublishAsync")]
    Task PublishAsync(LiveEvent liveEvent);

    /// <summary>Subscribes an observer. Subscriptions expire, so the caller has to repeat the call regularly.</summary>
    [Alias("SubscribeAsync")]
    Task SubscribeAsync(ILiveEventObserver observer);

    [Alias("UnsubscribeAsync")]
    Task UnsubscribeAsync(ILiveEventObserver observer);
}

public static class ClusterEventKeys
{
    public const string Hub = "hub";
}
