using GleisGrain.Trips.Contracts;

namespace GleisGrain.Trips.Domain;

/// <summary>
/// One departure of a line at a stop. Pure domain model: no Orleans runtime, no I/O.
/// The delay follows the reports of the stop until the trip has departed, from then on it is final.
/// </summary>
internal sealed class Trip(
    TripId id,
    string stopId,
    string line,
    string direction,
    DateTimeOffset plannedAt,
    int delayMinutes,
    DateTimeOffset lastObservedAt)
{
    public TripId Id { get; } = id;

    public string StopId { get; } = stopId;

    public string Line { get; } = line;

    public string Direction { get; } = direction;

    public DateTimeOffset PlannedAt { get; } = plannedAt;

    public int DelayMinutes { get; private set; } = delayMinutes;

    public DateTimeOffset LastObservedAt { get; private set; } = lastObservedAt;

    /// <summary>Planned time plus delay.</summary>
    public DateTimeOffset ExpectedAt => PlannedAt.AddMinutes(DelayMinutes);

    /// <summary>The trip as seen in the first report.</summary>
    public static Trip Start(TripId id, TripReport report) =>
        new(id, report.StopId, report.Line, report.Direction, report.PlannedAt, report.DelayMinutes, report.ObservedAt);

    public bool HasDeparted(DateTimeOffset now) => ExpectedAt <= now;

    /// <summary>
    /// Takes a later observation of the delay. Reports older than the last observation and reports observed
    /// after the trip departed are ignored. Returns true when the delay changed.
    /// </summary>
    public bool Report(TripReport report)
    {
        if (report.ObservedAt < LastObservedAt || ExpectedAt <= report.ObservedAt)
        {
            return false;
        }

        LastObservedAt = report.ObservedAt;

        if (report.DelayMinutes == DelayMinutes)
        {
            return false;
        }

        DelayMinutes = report.DelayMinutes;

        return true;
    }

    public TripSnapshot ToSnapshot(DateTimeOffset now) =>
        new(Id, StopId, Line, Direction, PlannedAt, DelayMinutes, HasDeparted(now));
}
