using GleisGrain.Cluster.Contracts;
using GleisGrain.Cluster.Events;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace GleisGrain.Cluster.Tests;

/// <summary>The reason the module exists: an event raised through one silo reaches the browsers of every silo.</summary>
public sealed class CrossSiloTests : IAsyncLifetime
{
    private TestCluster _cluster = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = new TestClusterBuilder(2);
        builder.AddSiloBuilderConfigurator<Configurator>();
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    public async ValueTask DisposeAsync() => await _cluster.DisposeAsync();

    [Fact]
    public async Task Publish_ThroughTheClient_ReachesTheBroadcasterOfEverySilo()
    {
        var broadcasters = _cluster.Silos
            .Cast<InProcessSiloHandle>()
            .Select(silo => silo.SiloHost.Services.GetRequiredService<LiveEventBroadcaster>())
            .ToList();
        Assert.Equal(2, broadcasters.Count);
        Assert.NotSame(broadcasters[0], broadcasters[1]);

        using var browserOnSiloA = broadcasters[0].Subscribe();
        using var browserOnSiloB = broadcasters[1].Subscribe();
        var liveEvent = LiveEvent.Of(new BetPlaced("Max", "8f3a1c", "1"));
        var hub = _cluster.GrainFactory.GetGrain<IClusterEventsGrain>(ClusterEventKeys.Hub);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        var both = Task.WhenAll(
            browserOnSiloA.Reader.ReadAsync(timeout.Token).AsTask(),
            browserOnSiloB.Reader.ReadAsync(timeout.Token).AsTask());

        // The silos subscribe themselves in the background shortly after they start, so publish until both got it.
        while (!both.IsCompleted)
        {
            await hub.PublishAsync(liveEvent);
            await Task.WhenAny(both, Task.Delay(100, timeout.Token));
        }

        Assert.All(await both, received => Assert.Equal(liveEvent, received));
    }

    public sealed class Configurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) =>
            siloBuilder.ConfigureServices(services => services.AddClusterServices());
    }
}
