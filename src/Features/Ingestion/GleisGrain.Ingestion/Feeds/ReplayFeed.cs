using GleisGrain.Ingestion.Contracts;
using GleisGrain.Ingestion.Domain;

namespace GleisGrain.Ingestion.Feeds;

/// <summary>
/// Serves recorded EFA answers (embedded resources, see Replay/record.sh). Departure times are shifted by
/// the time that passed since the recording, so a replayed board always looks current.
/// </summary>
internal sealed class ReplayFeed(TimeProvider timeProvider, ILogger<ReplayFeed> logger) : IEfaFeed
{
    public Task<IReadOnlyList<DepartureSnapshot>> FetchDeparturesAsync(string efaStopId, CancellationToken cancellationToken)
    {
        var xml = ReadRecording($"Replay/departures/{efaStopId}.xml");
        if (xml is null)
        {
            logger.NoRecording(efaStopId);
            return Task.FromResult<IReadOnlyList<DepartureSnapshot>>([]);
        }

        var board = EfaParser.ParseDepartures(xml);
        var shift = timeProvider.GetUtcNow() - board.RequestedAt;

        return Task.FromResult<IReadOnlyList<DepartureSnapshot>>(
            board.Departures.Select(departure => departure with { PlannedAt = departure.PlannedAt + shift }).ToList());
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
