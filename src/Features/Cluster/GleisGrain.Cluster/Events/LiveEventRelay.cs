using GleisGrain.Cluster.Contracts;
using Microsoft.Extensions.Options;

namespace GleisGrain.Cluster.Events;

/// <summary>
/// Makes an event raised on silo A reach a browser connected to silo B. Browsers are connected to one silo only,
/// but any silo can publish. So every silo registers one observer with the hub grain (which lives on one silo);
/// the hub calls every observer, and each observer hands the event to the <see cref="LiveEventBroadcaster"/>
/// of its own silo, which writes it to the browsers connected there.
/// </summary>
internal sealed class LiveEventRelay(
    IGrainFactory grains,
    LiveEventBroadcaster broadcaster,
    IOptions<ClusterOptions> options,
    TimeProvider time,
    ILogger<LiveEventRelay> logger) : BackgroundService
{
    private static readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(2);

    // Orleans keeps only a weak reference to a local observer, so the relay has to hold on to it.
    private readonly Observer _observer = new(broadcaster);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var renewEvery = options.Value.SubscriptionTtl / 3;
        var reference = grains.CreateObjectReference<ILiveEventObserver>(_observer);
        var hub = grains.GetGrain<IClusterEventsGrain>(ClusterEventKeys.Hub);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var wait = renewEvery;
                try
                {
                    await hub.SubscribeAsync(reference);
                    logger.SubscribedToHub(renewEvery);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    // The hub may be mid-move after a silo died, or this silo may still be starting. Try again soon.
                    wait = _retryDelay < renewEvery ? _retryDelay : renewEvery;
                    logger.SubscribeToHubFailed(exception, wait);
                }

                await Task.Delay(wait, time, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The silo is shutting down. The hub forgets this silo after the subscription expired.
        }
    }

    private sealed class Observer(LiveEventBroadcaster broadcaster) : ILiveEventObserver
    {
        public Task OnEvent(LiveEvent liveEvent)
        {
            broadcaster.Publish(liveEvent);

            return Task.CompletedTask;
        }
    }
}
