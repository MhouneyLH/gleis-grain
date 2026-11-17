using GleisGrain.Cluster.Contracts;
using GleisGrain.Ingestion.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Tests.Fakes;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Stops.Tests;

public sealed class StopGrainTests : IAsyncLifetime
{
    private const string Hbf = "60501720";

    private StopsCluster? _cluster;

    private StopsCluster Cluster => _cluster ?? throw new InvalidOperationException("Cluster not started.");

    private StopsFakes Fakes => Cluster.Fakes;

    private DateTimeOffset Now => Fakes.Time.GetUtcNow();

    public async ValueTask InitializeAsync() => _cluster = await StopsCluster.StartAsync();

    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetBoardAsync_FirstRequest_RefreshesFromTheSource()
    {
        Fakes.SetDepartures(Hbf, Departure("1", minutes: 5, delay: 2));

        var board = await Stop(Hbf).GetBoardAsync();

        Assert.NotNull(board);
        Assert.Equal("Hauptbahnhof", board.Name);
        Assert.Equal(Now, board.RefreshedAt);
        var departure = Assert.Single(board.Departures);
        Assert.Equal(new BoardDeparture(TripId.For(Hbf, "1", Now.AddMinutes(5)), "1", "Universität", Now.AddMinutes(5), 2, "A"), departure);
        Assert.Equal(1, Fakes.SourceCalls);
    }

    [Fact]
    public async Task GetBoardAsync_FreshBoard_DoesNotAskTheSourceAgain()
    {
        await Stop(Hbf).GetBoardAsync();
        Fakes.Time.Advance(TimeSpan.FromSeconds(29));

        await Stop(Hbf).GetBoardAsync();

        Assert.Equal(1, Fakes.SourceCalls);
    }

    [Fact]
    public async Task GetBoardAsync_BoardOlderThanMaxAge_RefreshesAgain()
    {
        Fakes.SetDepartures(Hbf, Departure("1", minutes: 5, delay: 0));
        await Stop(Hbf).GetBoardAsync();
        Fakes.Time.Advance(TimeSpan.FromSeconds(31));
        Fakes.SetDepartures(Hbf, Departure("1", minutes: 5, delay: 4));

        var board = await Stop(Hbf).GetBoardAsync();

        Assert.Equal(2, Fakes.SourceCalls);
        Assert.Equal(4, Assert.Single(board!.Departures).DelayMinutes);
        Assert.Equal(Now, board.RefreshedAt);
    }

    [Fact]
    public async Task GetBoardAsync_Refresh_ReportsEveryDepartureToItsTrip()
    {
        Fakes.SetDepartures(Hbf, Departure("1", minutes: 5, delay: 2), Departure("2", minutes: 8, delay: 0));

        await Stop(Hbf).GetBoardAsync();
        await StopsCluster.WaitUntilAsync(() => Fakes.Reports.Count == 2);

        Assert.Equivalent(
            new[]
            {
                new TripReport(Hbf, "1", "Universität", Now.AddMinutes(5), 2, Now),
                new TripReport(Hbf, "2", "Universität", Now.AddMinutes(8), 0, Now),
            },
            Fakes.Reports.ToArray());
    }

    [Fact]
    public async Task GetBoardAsync_SourceFails_KeepsTheOldBoardAndTriesAgainNextTime()
    {
        Fakes.SetDepartures(Hbf, Departure("1", minutes: 5, delay: 0));
        await Stop(Hbf).GetBoardAsync();
        Fakes.SourceFails = true;
        Fakes.Time.Advance(TimeSpan.FromSeconds(31));

        var stale = await Stop(Hbf).GetBoardAsync();
        Fakes.SourceFails = false;
        var recovered = await Stop(Hbf).GetBoardAsync();

        Assert.Single(stale!.Departures);
        Assert.Equal(3, Fakes.SourceCalls);
        Assert.Equal(Now, recovered!.RefreshedAt);
    }

    [Fact]
    public async Task GetBoardAsync_UnknownStop_ReturnsNullAndPublishesNothing()
    {
        var unknown = await Stop("nowhere").GetBoardAsync();
        await Stop(Hbf).GetBoardAsync();
        await StopsCluster.WaitUntilAsync(() => Fakes.Events.Any(e => e == LiveEvent.Of(new StopActivated(Hbf, "Primary"))));

        Assert.Null(unknown);
        Assert.DoesNotContain(Fakes.Events, e => e.Json.Contains("nowhere", StringComparison.Ordinal));
        Assert.Equal(1, Fakes.SourceCalls);
    }

    [Fact]
    public async Task WatchAsync_UnknownStop_ReturnsZeroAndPublishesNothing()
    {
        var count = await Stop("nowhere").WatchAsync("Max");
        await Stop(Hbf).WatchAsync("Max");
        await StopsCluster.WaitUntilAsync(() => Fakes.Events.Any(e => e.Type == StopWatchersChanged.EventType));

        Assert.Equal(0, count);
        Assert.DoesNotContain(Fakes.Events, e => e.Json.Contains("nowhere", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Activation_PublishesStopActivatedWithTheSiloName()
    {
        await Stop(Hbf).GetBoardAsync();

        await StopsCluster.WaitUntilAsync(() => !Fakes.Events.IsEmpty);

        Assert.Contains(LiveEvent.Of(new StopActivated(Hbf, "Primary")), Fakes.Events);
    }

    [Fact]
    public async Task Deactivation_PublishesStopDeactivated()
    {
        await Stop(Hbf).GetBoardAsync();

        await Cluster.Grains.GetGrain<IManagementGrain>(0).ForceActivationCollection(TimeSpan.Zero, TestContext.Current.CancellationToken);

        await StopsCluster.WaitUntilAsync(() => Fakes.Events.Contains(LiveEvent.Of(new StopDeactivated(Hbf))));
    }

    [Fact]
    public async Task WatchAsync_NewWatcher_ReturnsTheCountAndPublishesIt()
    {
        var first = await Stop(Hbf).WatchAsync("Max");
        var second = await Stop(Hbf).WatchAsync("Moritz");

        await StopsCluster.WaitUntilAsync(() => Fakes.Events.Contains(LiveEvent.Of(new StopWatchersChanged(Hbf, 2))));
        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Contains(LiveEvent.Of(new StopWatchersChanged(Hbf, 1)), Fakes.Events);
    }

    [Fact]
    public async Task WatchAsync_SameWatcherTwice_IsIdempotent()
    {
        await Stop(Hbf).WatchAsync("Max");
        var again = await Stop(Hbf).WatchAsync("max");
        await Stop(Hbf).WatchAsync("Moritz");

        await StopsCluster.WaitUntilAsync(() => Fakes.Events.Contains(LiveEvent.Of(new StopWatchersChanged(Hbf, 2))));
        Assert.Equal(1, again);
        Assert.Single(Fakes.Events, e => e.Type == StopWatchersChanged.EventType && e.Json.Contains(":1}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnwatchAsync_Watcher_ReturnsTheCountAndPublishesIt()
    {
        await Stop(Hbf).WatchAsync("Max");
        await Stop(Hbf).WatchAsync("Moritz");

        var count = await Stop(Hbf).UnwatchAsync("MAX");

        Assert.Equal(1, count);
        await StopsCluster.WaitUntilAsync(() => Fakes.Events.Count(e => e.Json.Contains(":1}", StringComparison.Ordinal)) == 2);
    }

    [Fact]
    public async Task UnwatchAsync_UnknownWatcher_IsIdempotent()
    {
        await Stop(Hbf).WatchAsync("Max");

        var count = await Stop(Hbf).UnwatchAsync("Moritz");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetBoardAsync_ShowsTheWatchers()
    {
        await Stop(Hbf).WatchAsync("Max");

        var board = await Stop(Hbf).GetBoardAsync();

        Assert.Equal(1, board!.Watchers);
    }

    [Fact]
    public async Task WatchAsync_AfterDeactivation_WatchersSurviveInStorage()
    {
        await Stop(Hbf).WatchAsync("Max");

        await Cluster.Grains.GetGrain<IManagementGrain>(0).ForceActivationCollection(TimeSpan.Zero, TestContext.Current.CancellationToken);
        var count = await Stop(Hbf).WatchAsync("Moritz");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Reminder_Fires_RefreshesTheBoardAndPersistsIt()
    {
        Fakes.SetDepartures(Hbf, Departure("1", minutes: 5, delay: 3));
        await Stop(Hbf).WatchAsync("Max");

        await Remindable(Hbf).ReceiveReminder("refresh", default, TestContext.Current.CancellationToken);
        await Cluster.Grains.GetGrain<IManagementGrain>(0).ForceActivationCollection(TimeSpan.Zero, TestContext.Current.CancellationToken);
        var board = await Stop(Hbf).GetBoardAsync();

        // The reminder refresh was written to storage: the reactivated grain serves it without asking the source again.
        Assert.Equal(1, Fakes.SourceCalls);
        Assert.Equal(3, Assert.Single(board!.Departures).DelayMinutes);
    }

    private IStopGrain Stop(string id) => Cluster.Grains.GetGrain<IStopGrain>(id);

    private IRemindable Remindable(string id) =>
        Cluster.Grains.GetGrain(GrainId.Create("stop", id)).AsReference<IRemindable>();

    private DepartureSnapshot Departure(string line, int minutes, int delay) =>
        new(line, "Universität", Now.AddMinutes(minutes), delay, "A");
}
