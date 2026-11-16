using GleisGrain.Cluster.Contracts;
using GleisGrain.Cluster.Events;
using Microsoft.Extensions.DependencyInjection;

namespace GleisGrain.Cluster.Tests;

public sealed class LiveEventBroadcasterTests
{
    private readonly LiveEventBroadcaster _broadcaster = new(new ClusterMetrics(
        new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>()));

    [Fact]
    public void Publish_WithTwoClients_DeliversToBoth()
    {
        using var first = _broadcaster.Subscribe();
        using var second = _broadcaster.Subscribe();
        var liveEvent = Event(1);

        _broadcaster.Publish(liveEvent);

        Assert.True(first.Reader.TryRead(out var receivedByFirst));
        Assert.True(second.Reader.TryRead(out var receivedBySecond));
        Assert.Equal(liveEvent, receivedByFirst);
        Assert.Equal(liveEvent, receivedBySecond);
    }

    [Fact]
    public void Publish_WhenAClientIsSlow_DropsItsOldestEvents()
    {
        using var slow = _broadcaster.Subscribe();
        var overflow = 10;

        for (var i = 0; i < LiveEventBroadcaster.ClientCapacity + overflow; i++)
        {
            _broadcaster.Publish(Event(i));
        }

        var received = new List<LiveEvent>();
        while (slow.Reader.TryRead(out var liveEvent))
        {
            received.Add(liveEvent);
        }

        Assert.Equal(LiveEventBroadcaster.ClientCapacity, received.Count);
        Assert.Equal(Event(overflow), received[0]);
        Assert.Equal(Event(LiveEventBroadcaster.ClientCapacity + overflow - 1), received[^1]);
    }

    [Fact]
    public void Publish_WhenOneClientIsSlow_DoesNotAffectTheOthers()
    {
        using var slow = _broadcaster.Subscribe();
        using var quick = _broadcaster.Subscribe();
        var received = new List<LiveEvent>();

        for (var i = 0; i < LiveEventBroadcaster.ClientCapacity * 2; i++)
        {
            _broadcaster.Publish(Event(i));
            Assert.True(quick.Reader.TryRead(out var liveEvent));
            received.Add(liveEvent);
        }

        Assert.Equal(LiveEventBroadcaster.ClientCapacity * 2, received.Count);
    }

    [Fact]
    public void Dispose_RemovesTheClientAndCompletesItsReader()
    {
        var removed = _broadcaster.Subscribe();

        removed.Dispose();
        _broadcaster.Publish(Event(1));

        Assert.False(removed.Reader.TryRead(out _));
        Assert.True(removed.Reader.Completion.IsCompleted);
    }

    private static LiveEvent Event(int number) => LiveEvent.Of(new StopWatchersChanged("8f3a1c", number));
}
