using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using GleisGrain.Ingestion.Domain;
using GleisGrain.Ingestion.Feeds;

namespace GleisGrain.Ingestion.Tests;

public class ReplayFeedTests
{
    private const string RecordedStop = "60500920";

    [Fact]
    public async Task FetchDepartures_KeepsTheGapsOfTheRecording()
    {
        var recorded = EfaParser.ParseDepartures(EfaXml.ReadRecording($"Replay/departures/{RecordedStop}.xml")).Departures.OrderBy(d => d.PlannedAt).ToList();
        var feed = FeedAt(DateTimeOffset.UnixEpoch);

        var replayed = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        Assert.Equal(recorded.Count, replayed.Count);
        Assert.Equal(
            recorded.Select(d => d.PlannedAt - recorded[0].PlannedAt),
            replayed.Select(d => d.PlannedAt - replayed[0].PlannedAt));
        Assert.Equal(recorded.Select(d => d.Line), replayed.Select(d => d.Line));
        Assert.Equal(TimeSpan.FromMinutes(1), replayed[0].PlannedAt - DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public async Task FetchDepartures_AsTimePasses_ADepartureKeepsItsTimeAndLeavesTheBoard()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var feed = new ReplayFeed(clock, NullLogger<ReplayFeed>.Instance);
        var before = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        clock.Advance(TimeSpan.FromSeconds(90));
        var after = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(before[0], after);
        Assert.Equal(before[1], after[0]);
        Assert.All(after, departure => Assert.True(departure.PlannedAt > clock.GetUtcNow()));
    }

    [Fact]
    public async Task FetchDepartures_TwoCallsSecondsApart_ReturnTheSameDepartures()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch + TimeSpan.FromHours(5));
        var feed = new ReplayFeed(clock, NullLogger<ReplayFeed>.Instance);
        var first = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        clock.Advance(TimeSpan.FromSeconds(7));
        var second = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task FetchDepartures_AfterTheLastDeparture_StartsTheLoopAgain()
    {
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var feed = new ReplayFeed(clock, NullLogger<ReplayFeed>.Instance);
        var firstLoop = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        clock.SetUtcNow(firstLoop[^1].PlannedAt + TimeSpan.FromSeconds(1));
        var board = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        Assert.Equal(firstLoop.Count, board.Count);
        Assert.Equal(firstLoop[0].Line, board[0].Line);
        Assert.True(board[0].PlannedAt > firstLoop[^1].PlannedAt);
    }

    [Fact]
    public async Task FetchDepartures_StopWithoutRecording_IsAnEmptyBoard()
    {
        var feed = FeedAt(DateTimeOffset.UnixEpoch);

        var replayed = await feed.FetchDeparturesAsync("00000000", TestContext.Current.CancellationToken);

        Assert.Empty(replayed);
    }

    [Fact]
    public async Task FetchNotices_ReturnsTheRecordedNotices()
    {
        var feed = FeedAt(DateTimeOffset.UnixEpoch);

        var notices = await feed.FetchNoticesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            EfaParser.ParseNotices(EfaXml.ReadRecording("Replay/notices.xml")).Select(notice => notice.Id),
            notices.Select(notice => notice.Id));
    }

    [Fact]
    public async Task FetchDepartures_EveryStopOfTheCatalogue_HasARecordingWithDepartures()
    {
        var catalogue = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "stops.json"), TestContext.Current.CancellationToken);
        var stopIds = JsonDocument.Parse(catalogue).RootElement.EnumerateArray().Select(stop => stop.GetProperty("id").GetString()!).ToList();
        var feed = FeedAt(DateTimeOffset.UnixEpoch);

        Assert.NotEmpty(stopIds);
        foreach (var stopId in stopIds)
        {
            var departures = await feed.FetchDeparturesAsync(stopId, TestContext.Current.CancellationToken);

            Assert.True(departures.Count > 0, $"Stop {stopId} has no recorded departures.");
        }
    }

    private static ReplayFeed FeedAt(DateTimeOffset now) =>
        new(new FakeTimeProvider(now), NullLogger<ReplayFeed>.Instance);
}
