using System.Threading.Channels;
using GleisGrain.Cluster.Contracts;

namespace GleisGrain.Cluster.Tests.Support;

/// <summary>An observer for tests: remembers what it was told and lets the test wait for it.</summary>
public sealed class EventCollector : ILiveEventObserver
{
    private readonly Channel<LiveEvent> _events = Channel.CreateUnbounded<LiveEvent>();

    public Task OnEvent(LiveEvent liveEvent)
    {
        _events.Writer.TryWrite(liveEvent);

        return Task.CompletedTask;
    }

    /// <summary>Waits for the next event. Fails the test after a generous timeout instead of hanging.</summary>
    public async Task<LiveEvent> NextAsync()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        return await _events.Reader.ReadAsync(timeout.Token);
    }

    public bool HasReceived() => _events.Reader.TryPeek(out _);
}
