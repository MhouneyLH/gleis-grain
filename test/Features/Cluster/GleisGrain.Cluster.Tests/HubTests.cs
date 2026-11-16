using GleisGrain.Cluster.Contracts;
using GleisGrain.Cluster.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Orleans.TestingHost;

namespace GleisGrain.Cluster.Tests;

public sealed class HubTests : IAsyncLifetime
{
    private static readonly TimeSpan _ttl = TimeSpan.FromSeconds(10);

    // Static because the test cluster creates the silo configurator by type. The clock only moves forward, and every
    // test uses a hub grain of its own, so tests do not disturb each other.
    private static readonly FakeTimeProvider _clock = new();

    private TestCluster _cluster = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<Configurator>();
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    public async ValueTask DisposeAsync() => await _cluster.DisposeAsync();

    [Fact]
    public async Task Publish_ToSubscribedObserver_DeliversTheEvent()
    {
        var hub = NewHub();
        var observer = new EventCollector();
        await hub.SubscribeAsync(Reference(observer));
        var liveEvent = LiveEvent.Of(new StopWatchersChanged("8f3a1c", 12));

        await hub.PublishAsync(liveEvent);

        Assert.Equal(liveEvent, await observer.NextAsync());
    }

    [Fact]
    public async Task Publish_AfterUnsubscribe_DoesNotDeliverTheEvent()
    {
        var hub = NewHub();
        var gone = new EventCollector();
        var stays = new EventCollector();
        var goneReference = Reference(gone);
        await hub.SubscribeAsync(goneReference);
        await hub.SubscribeAsync(Reference(stays));
        await hub.UnsubscribeAsync(goneReference);

        await PublishTwiceAsync(hub, stays);

        Assert.False(gone.HasReceived());
    }

    [Fact]
    public async Task Publish_AfterTheSubscriptionExpired_DoesNotDeliverTheEvent()
    {
        var hub = NewHub();
        var expired = new EventCollector();
        var fresh = new EventCollector();
        await hub.SubscribeAsync(Reference(expired));
        _clock.Advance(_ttl + TimeSpan.FromSeconds(1));
        await hub.SubscribeAsync(Reference(fresh));

        await PublishTwiceAsync(hub, fresh);

        Assert.False(expired.HasReceived());
    }

    [Fact]
    public async Task Publish_AfterTheSubscriptionWasRenewed_DeliversTheEvent()
    {
        var hub = NewHub();
        var observer = new EventCollector();
        var reference = Reference(observer);
        await hub.SubscribeAsync(reference);
        _clock.Advance(_ttl / 2 + TimeSpan.FromSeconds(1));
        await hub.SubscribeAsync(reference);
        _clock.Advance(_ttl / 2 + TimeSpan.FromSeconds(1));
        var liveEvent = LiveEvent.Of(new StopDeactivated("8f3a1c"));

        await hub.PublishAsync(liveEvent);

        Assert.Equal(liveEvent, await observer.NextAsync());
    }

    private IClusterEventsGrain NewHub() =>
        _cluster.GrainFactory.GetGrain<IClusterEventsGrain>($"hub-{Guid.NewGuid()}");

    private ILiveEventObserver Reference(EventCollector observer) =>
        _cluster.GrainFactory.CreateObjectReference<ILiveEventObserver>(observer);

    // Publishing is one-way, so the only way to know a dropped observer really was skipped is to wait until a live
    // observer got a later event: the hub handled both events by then.
    private static async Task PublishTwiceAsync(IClusterEventsGrain hub, EventCollector live)
    {
        await hub.PublishAsync(LiveEvent.Of(new StopDeactivated("1")));
        await live.NextAsync();
        await hub.PublishAsync(LiveEvent.Of(new StopDeactivated("2")));
        await live.NextAsync();
    }

    public sealed class Configurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) =>
            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<TimeProvider>(_clock);
                services.AddClusterServices();
                services.AddSingleton<IOptions<ClusterOptions>>(Options.Create(new ClusterOptions { SubscriptionTtl = _ttl }));
            });
    }
}
