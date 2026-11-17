using GleisGrain.Ingestion.Contracts;
using GleisGrain.Ingestion.Domain;

namespace GleisGrain.Ingestion.Feeds;

/// <summary>
/// Serves recorded EFA answers (embedded resources, see Replay/record.sh) as a loop on a fixed clock.
/// The first recorded departure of a stop leaves a minute after the loop starts, the others keep their gaps,
/// and the loop starts again once the last one has left. The board is a pure function of the time, so every silo
/// and every restart shows the same departures with the same trip ids, and a departure really does leave when its
/// time has come, which is what the bets are resolved against.
/// </summary>
internal sealed class ReplayFeed(TimeProvider timeProvider, ILogger<ReplayFeed> logger) : IEfaFeed
{
    private static readonly DateTimeOffset _loopEpoch = DateTimeOffset.UnixEpoch;
    private static readonly TimeSpan _lead = TimeSpan.FromMinutes(1);

    public Task<IReadOnlyList<DepartureSnapshot>> FetchDeparturesAsync(string efaStopId, CancellationToken cancellationToken)
    {
        var xml = ReadRecording($"Replay/departures/{efaStopId}.xml");
        if (xml is null)
        {
            logger.NoRecording(efaStopId);
            return Task.FromResult<IReadOnlyList<DepartureSnapshot>>([]);
        }

        var recorded = EfaParser.ParseDepartures(xml).Departures.OrderBy(departure => departure.PlannedAt).ToList();
        if (recorded.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<DepartureSnapshot>>([]);
        }

        // Gaps between the recorded departures, the first one after the lead. A loop ends one lead after the last.
        var first = recorded[0].PlannedAt;
        var offsets = recorded.Select(departure => (Departure: departure, Offset: departure.PlannedAt - first + _lead)).ToList();
        var loop = offsets[^1].Offset + _lead;

        var now = timeProvider.GetUtcNow();
        var loopNumber = (long)Math.Floor((now - _loopEpoch) / loop);
        var loopStart = _loopEpoch + TimeSpan.FromTicks(loop.Ticks * loopNumber);

        // What is still ahead in this loop, completed by the start of the next one like a real board.
        var upcoming = Playback(loopStart).Concat(Playback(loopStart + loop))
            .Where(departure => departure.PlannedAt > now)
            .Take(recorded.Count)
            .ToList();

        return Task.FromResult<IReadOnlyList<DepartureSnapshot>>(upcoming);

        IEnumerable<DepartureSnapshot> Playback(DateTimeOffset start) =>
            offsets.Select(item => item.Departure with { PlannedAt = start + item.Offset });
    }

    public Task<IReadOnlyList<NoticeSnapshot>> FetchNoticesAsync(CancellationToken cancellationToken) =>
        Task.FromResult(EfaParser.ParseNotices(ReadRecording("Replay/notices.xml")
            ?? throw new InvalidOperationException("The recording Replay/notices.xml is missing.")));

    private static string? ReadRecording(string name)
    {
        using var stream = typeof(ReplayFeed).Assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
