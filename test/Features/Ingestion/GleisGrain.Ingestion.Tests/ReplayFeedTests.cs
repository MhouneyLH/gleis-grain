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
    public async Task FetchDepartures_LaterThanTheRecording_ShiftsEveryTimeByTheElapsedTime()
    {
        var recorded = EfaParser.ParseDepartures(EfaXml.ReadRecording($"Replay/departures/{RecordedStop}.xml"));
        var elapsed = TimeSpan.FromHours(26) + TimeSpan.FromMinutes(7);
        var feed = FeedAt(recorded.RequestedAt + elapsed);

        var replayed = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        Assert.Equal(recorded.Departures.Count, replayed.Count);
        Assert.All(recorded.Departures.Zip(replayed), pair =>
        {
            Assert.Equal(pair.First.PlannedAt + elapsed, pair.Second.PlannedAt);
            Assert.Equal(pair.First with { PlannedAt = pair.Second.PlannedAt }, pair.Second);
        });
    }

    [Fact]
    public async Task FetchDepartures_AtTheTimeOfTheRecording_ReturnsTheRecordedTimes()
    {
        var recorded = EfaParser.ParseDepartures(EfaXml.ReadRecording($"Replay/departures/{RecordedStop}.xml"));
        var feed = FeedAt(recorded.RequestedAt);

        var replayed = await feed.FetchDeparturesAsync(RecordedStop, TestContext.Current.CancellationToken);

        Assert.Equal(recorded.Departures, replayed);
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
