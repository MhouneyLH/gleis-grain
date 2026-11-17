using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Domain;
using GleisGrain.Stops.Tests.Fakes;

namespace GleisGrain.Stops.Tests;

public sealed class StopBoardCacheGrainTests : IAsyncLifetime
{
    private const string Hbf = "60501720";

    private StopsCluster? _cluster;

    private StopsCluster Cluster => _cluster ?? throw new InvalidOperationException("Cluster not started.");

    public async ValueTask InitializeAsync() => _cluster = await StopsCluster.StartAsync();

    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetBoardAsync_WithinTheTtl_ServesTheCachedSnapshot()
    {
        var cache = Cluster.Grains.GetGrain<IStopBoardCacheGrain>(Hbf);
        await cache.GetBoardAsync();

        // The stop grain has a new watcher, but the cache keeps the snapshot it took.
        await Cluster.Grains.GetGrain<IStopGrain>(Hbf).WatchAsync("Max");
        var cached = await cache.GetBoardAsync();

        Assert.Equal(0, cached!.Watchers);
    }

    [Fact]
    public async Task GetBoardAsync_AfterTheTtl_AsksTheStopGrainAgain()
    {
        var cache = Cluster.Grains.GetGrain<IStopBoardCacheGrain>(Hbf);
        await cache.GetBoardAsync();
        await Cluster.Grains.GetGrain<IStopGrain>(Hbf).WatchAsync("Max");

        Cluster.Fakes.Time.Advance(TimeSpan.FromSeconds(6));
        var fresh = await cache.GetBoardAsync();

        Assert.Equal(1, fresh!.Watchers);
    }

    [Fact]
    public async Task GetBoardAsync_ManyReadsWithinTheTtl_AskTheSourceOnce()
    {
        var cache = Cluster.Grains.GetGrain<IStopBoardCacheGrain>(Hbf);

        for (var i = 0; i < 10; i++)
        {
            await cache.GetBoardAsync();
        }

        Assert.Equal(1, Cluster.Fakes.SourceCalls);
    }

    [Fact]
    public async Task GetBoardAsync_UnknownStop_ReturnsNull()
    {
        var board = await Cluster.Grains.GetGrain<IStopBoardCacheGrain>("nowhere").GetBoardAsync();

        Assert.Null(board);
    }
}
