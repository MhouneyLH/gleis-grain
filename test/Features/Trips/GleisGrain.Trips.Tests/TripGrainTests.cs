using GleisGrain.Trips.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using Orleans.TestingHost;

namespace GleisGrain.Trips.Tests;

public sealed class TripGrainTests : IAsyncLifetime
{
    // Orleans reads the test clock too, so it starts at the real time instead of a date hours away.
    private static readonly DateTimeOffset _plannedAt = StartOfMinute(DateTimeOffset.UtcNow.AddMinutes(20));

    private readonly FakeTimeProvider _time = new(_plannedAt.AddMinutes(-20));
    private readonly string _key = Guid.NewGuid().ToString("N");
    private TestCluster? _cluster;

    private TestCluster Cluster => _cluster ?? throw new InvalidOperationException("Cluster not started.");

    public async ValueTask InitializeAsync()
    {
        TripsSiloConfigurator.Clocks[_key] = _time;

        var builder = new TestClusterBuilder(1);
        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { [TripsSiloConfigurator.KeySetting] = _key }));
        builder.AddSiloBuilderConfigurator<TripsSiloConfigurator>();

        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.StopAllSilosAsync();
            await _cluster.DisposeAsync();
        }

        TripsSiloConfigurator.Clocks.TryRemove(_key, out _);
    }

    [Fact]
    public async Task GetAsync_NothingReported_ReturnsNull()
    {
        var trip = await Grain("never-reported").GetAsync();

        Assert.Null(trip);
    }

    [Fact]
    public async Task ReportAsync_FirstReport_CreatesTheTrip()
    {
        var grain = Grain("t-first");

        await grain.ReportAsync(Report(delay: 2));
        var trip = await Eventually(grain);

        Assert.Equal(new TripSnapshot(TripId.From("t-first"), "60501720", "1", "Universität", _plannedAt, 2, HasDeparted: false), trip);
    }

    [Fact]
    public async Task ReportAsync_LaterReport_UpdatesTheDelay()
    {
        var grain = Grain("t-update");
        await grain.ReportAsync(Report(delay: 2));
        await Eventually(grain);

        _time.Advance(TimeSpan.FromMinutes(5));
        await grain.ReportAsync(Report(delay: 4));
        var trip = await Eventually(grain, expectedDelay: 4);

        Assert.Equal(4, trip.DelayMinutes);
    }

    [Fact]
    public async Task ReportAsync_AfterDeparture_KeepsTheFinalDelay()
    {
        var grain = Grain("t-final");
        await grain.ReportAsync(Report(delay: 2));
        await Eventually(grain);

        _time.SetUtcNow(_plannedAt.AddMinutes(3));
        await grain.ReportAsync(Report(delay: 9));
        var trip = await grain.GetAsync();

        Assert.NotNull(trip);
        Assert.Equal(2, trip.DelayMinutes);
        Assert.True(trip.HasDeparted);
    }

    [Fact]
    public async Task GetAsync_AfterTheDeparture_ReportsDeparted()
    {
        var grain = Grain("t-departed");
        await grain.ReportAsync(Report(delay: 1));
        var before = await Eventually(grain);

        _time.SetUtcNow(_plannedAt.AddMinutes(1));
        var after = await grain.GetAsync();

        Assert.False(before.HasDeparted);
        Assert.NotNull(after);
        Assert.True(after.HasDeparted);
    }

    [Fact]
    public async Task ReportAsync_AfterTheGrainWasDeactivated_RestoresTheStateFromStorage()
    {
        var grain = Grain("t-restore");
        await grain.ReportAsync(Report(delay: 3));
        await Eventually(grain);

        await Cluster.Client.GetGrain<IManagementGrain>(0).ForceActivationCollection(TimeSpan.Zero, TestContext.Current.CancellationToken);
        var trip = await grain.GetAsync();

        Assert.NotNull(trip);
        Assert.Equal(3, trip.DelayMinutes);
    }

    private static DateTimeOffset StartOfMinute(DateTimeOffset value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, TimeSpan.Zero);

    private ITripGrain Grain(string id) => Cluster.Client.GetGrain<ITripGrain>(id);

    private TripReport Report(int delay) =>
        new("60501720", "1", "Universität", _plannedAt, delay, _time.GetUtcNow());

    /// <summary>ReportAsync is one-way, so the effect shows up shortly after the call returns.</summary>
    private static async Task<TripSnapshot> Eventually(ITripGrain grain, int? expectedDelay = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (true)
        {
            var trip = await grain.GetAsync();
            if (trip is not null && (expectedDelay is null || trip.DelayMinutes == expectedDelay))
            {
                return trip;
            }

            await Task.Delay(20, timeout.Token);
        }
    }
}
