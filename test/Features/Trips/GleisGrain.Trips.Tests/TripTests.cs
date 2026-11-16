using GleisGrain.Trips.Contracts;
using GleisGrain.Trips.Domain;

namespace GleisGrain.Trips.Tests;

public class TripTests
{
    private static readonly DateTimeOffset _plannedAt = new(2026, 11, 17, 10, 5, 0, TimeSpan.Zero);

    [Fact]
    public void Start_FirstReport_TakesItsFacts()
    {
        var trip = Trip.Start(TripId.From("t1"), Report(delay: 2, observedAt: _plannedAt.AddMinutes(-10)));

        Assert.Equal("60501720", trip.StopId);
        Assert.Equal("1", trip.Line);
        Assert.Equal("Universität", trip.Direction);
        Assert.Equal(_plannedAt, trip.PlannedAt);
        Assert.Equal(2, trip.DelayMinutes);
    }

    [Fact]
    public void Report_NewerObservation_UpdatesDelay()
    {
        var trip = StartedTrip(delay: 2, observedAt: _plannedAt.AddMinutes(-10));

        var changed = trip.Report(Report(delay: 4, observedAt: _plannedAt.AddMinutes(-5)));

        Assert.True(changed);
        Assert.Equal(4, trip.DelayMinutes);
    }

    [Fact]
    public void Report_SameDelay_IsNotAChange()
    {
        var trip = StartedTrip(delay: 2, observedAt: _plannedAt.AddMinutes(-10));

        var changed = trip.Report(Report(delay: 2, observedAt: _plannedAt.AddMinutes(-5)));

        Assert.False(changed);
        Assert.Equal(_plannedAt.AddMinutes(-5), trip.LastObservedAt);
    }

    [Fact]
    public void Report_OlderThanLastObservation_IsIgnored()
    {
        var trip = StartedTrip(delay: 2, observedAt: _plannedAt.AddMinutes(-5));

        var changed = trip.Report(Report(delay: 6, observedAt: _plannedAt.AddMinutes(-8)));

        Assert.False(changed);
        Assert.Equal(2, trip.DelayMinutes);
        Assert.Equal(_plannedAt.AddMinutes(-5), trip.LastObservedAt);
    }

    [Fact]
    public void Report_ObservedAfterDeparture_IsIgnoredSoTheDelayIsFinal()
    {
        var trip = StartedTrip(delay: 2, observedAt: _plannedAt.AddMinutes(-1));

        var changed = trip.Report(Report(delay: 9, observedAt: _plannedAt.AddMinutes(2)));

        Assert.False(changed);
        Assert.Equal(2, trip.DelayMinutes);
    }

    [Fact]
    public void Report_ObservedBeforeDeparture_StillCountsWhenItPostponesIt()
    {
        var trip = StartedTrip(delay: 0, observedAt: _plannedAt.AddMinutes(-10));

        var changed = trip.Report(Report(delay: 3, observedAt: _plannedAt.AddSeconds(-30)));

        Assert.True(changed);
        Assert.Equal(_plannedAt.AddMinutes(3), trip.ExpectedAt);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void HasDeparted_ComparesPlannedTimePlusDelayWithNow(int minutesAfterExpected, bool expected)
    {
        var trip = StartedTrip(delay: 3, observedAt: _plannedAt.AddMinutes(-10));

        Assert.Equal(expected, trip.HasDeparted(_plannedAt.AddMinutes(3 + minutesAfterExpected)));
    }

    [Fact]
    public void ToSnapshot_AfterDeparture_ShowsFinalDelayAndDepartedFlag()
    {
        var trip = StartedTrip(delay: 2, observedAt: _plannedAt.AddMinutes(-1));
        var now = _plannedAt.AddMinutes(5);

        var snapshot = trip.ToSnapshot(now);

        Assert.Equal(new TripSnapshot(TripId.From("t1"), "60501720", "1", "Universität", _plannedAt, 2, HasDeparted: true), snapshot);
    }

    private static Trip StartedTrip(int delay, DateTimeOffset observedAt) =>
        Trip.Start(TripId.From("t1"), Report(delay, observedAt));

    private static TripReport Report(int delay, DateTimeOffset observedAt) =>
        new("60501720", "1", "Universität", _plannedAt, delay, observedAt);
}
