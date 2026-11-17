using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Tests.Fakes;

namespace GleisGrain.Stops.Tests;

/// <summary>Grain timers run on the system clock, so these tests use a short period and poll.</summary>
public sealed class StopGrainTimerTests : IAsyncLifetime
{
    private const string Hbf = "60501720";

    private StopsCluster? _cluster;

    private StopsCluster Cluster => _cluster ?? throw new InvalidOperationException("Cluster not started.");

    public async ValueTask InitializeAsync() =>
        _cluster = await StopsCluster.StartAsync(new Dictionary<string, string?> { ["WatchedRefreshPeriod"] = "00:00:01" });

    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.DisposeAsync();
        }
    }

    [Fact]
    public async Task WatchAsync_FirstWatcher_StartsTheRefreshTimer()
    {
        await Cluster.Grains.GetGrain<IStopGrain>(Hbf).WatchAsync("Max");

        // Nobody asked for the board, so only the timer can call the source.
        await StopsCluster.WaitUntilAsync(() => Cluster.Fakes.SourceCalls >= 1);
    }
}
