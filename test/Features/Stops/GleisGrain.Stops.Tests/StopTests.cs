using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Domain;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Stops.Tests;

public class StopTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly StopInfo _info = new(StopId.From("60501720"), "Hauptbahnhof", 48.291028, 14.291325);

    [Fact]
    public void Watch_NewWatcher_ReturnsTrue()
    {
        var stop = new Stop(_info);

        Assert.True(stop.Watch("Max"));
        Assert.Single(stop.Watchers);
    }

    [Theory]
    [InlineData("Max")]
    [InlineData("max")]
    [InlineData("MAX")]
    public void Watch_SameWatcherInAnyCase_ReturnsFalse(string again)
    {
        var stop = new Stop(_info);
        stop.Watch("Max");

        Assert.False(stop.Watch(again));
        Assert.Single(stop.Watchers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Watch_BlankWatcher_Throws(string watcher) =>
        Assert.Throws<ArgumentException>(() => new Stop(_info).Watch(watcher));

    [Fact]
    public void Unwatch_KnownWatcherInAnyCase_RemovesIt()
    {
        var stop = new Stop(_info);
        stop.Watch("Max");

        Assert.True(stop.Unwatch("MAX"));
        Assert.Empty(stop.Watchers);
    }

    [Fact]
    public void Unwatch_UnknownWatcher_ReturnsFalse()
    {
        var stop = new Stop(_info);
        stop.Watch("Max");

        Assert.False(stop.Unwatch("Moritz"));
        Assert.Single(stop.Watchers);
    }

    [Fact]
    public void NeedsRefresh_NeverRefreshed_ReturnsTrue() =>
        Assert.True(new Stop(_info).NeedsRefresh(_now, TimeSpan.FromSeconds(30)));

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, true)]
    [InlineData(31, true)]
    public void NeedsRefresh_ComparesTheAgeWithTheMaximum(int ageSeconds, bool expected)
    {
        var stop = new Stop(_info);
        stop.UpdateBoard([], _now);

        Assert.Equal(expected, stop.NeedsRefresh(_now.AddSeconds(ageSeconds), TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void UpdateBoard_ReplacesTheBoardAndOrdersItByPlannedTime()
    {
        var stop = new Stop(_info);
        stop.UpdateBoard([Departure("1", 10), Departure("2", 5)], _now);

        stop.UpdateBoard([Departure("3", 20), Departure("1", 10), Departure("2", 5)], _now.AddSeconds(30));

        Assert.Equal(["2", "1", "3"], stop.Board.Select(d => d.Line));
        Assert.Equal(_now.AddSeconds(30), stop.RefreshedAt);
    }

    [Fact]
    public void ToSnapshot_ShowsStopWatchersAndBoard()
    {
        var stop = new Stop(_info);
        stop.Watch("Max");
        stop.Watch("Moritz");
        stop.UpdateBoard([Departure("1", 5)], _now);

        var snapshot = stop.ToSnapshot();

        Assert.Equal(_info.Id, snapshot.Id);
        Assert.Equal("Hauptbahnhof", snapshot.Name);
        Assert.Equal(48.291028, snapshot.Latitude);
        Assert.Equal(14.291325, snapshot.Longitude);
        Assert.Equal(2, snapshot.Watchers);
        Assert.Equal(_now, snapshot.RefreshedAt);
        var departure = Assert.Single(snapshot.Departures);
        Assert.Equal(new BoardDeparture(TripId.From("60501720:1:202611171005"), "1", "Universität", _now.AddMinutes(5), 2, "A"), departure);
    }

    [Fact]
    public void ToSnapshot_NeverRefreshed_HasNoRefreshTimeAndNoDepartures()
    {
        var snapshot = new Stop(_info).ToSnapshot();

        Assert.Null(snapshot.RefreshedAt);
        Assert.Empty(snapshot.Departures);
    }

    [Fact]
    public void StopState_RoundTrip_KeepsWatchersBoardAndRefreshTime()
    {
        var stop = new Stop(_info);
        stop.Watch("Max");
        stop.UpdateBoard([Departure("1", 5)], _now);

        var restored = StopState.Of(stop).ToStop(_info);

        Assert.Equal(["Max"], restored.Watchers);
        Assert.Equal(stop.Board, restored.Board);
        Assert.Equal(_now, restored.RefreshedAt);
    }

    private static Departure Departure(string line, int minutesFromNow) =>
        new(TripId.For("60501720", line, _now.AddMinutes(minutesFromNow)), line, "Universität", _now.AddMinutes(minutesFromNow), 2, "A");
}
