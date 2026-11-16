using GleisGrain.Trips.Contracts;

namespace GleisGrain.Trips.Domain;

/// <summary>Persisted blob. Only add fields with new ids; bump <see cref="CurrentSchemaVersion"/> and migrate on activation.</summary>
[GenerateSerializer, Alias("GleisGrain.Trips.Domain.TripState")]
internal sealed class TripState
{
    public const int CurrentSchemaVersion = 1;

    [Id(0)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [Id(1)]
    public string StopId { get; set; } = string.Empty;

    [Id(2)]
    public string Line { get; set; } = string.Empty;

    [Id(3)]
    public string Direction { get; set; } = string.Empty;

    [Id(4)]
    public DateTimeOffset PlannedAt { get; set; }

    [Id(5)]
    public int DelayMinutes { get; set; }

    [Id(6)]
    public DateTimeOffset LastObservedAt { get; set; }

    public static TripState Of(Trip trip) => new()
    {
        StopId = trip.StopId,
        Line = trip.Line,
        Direction = trip.Direction,
        PlannedAt = trip.PlannedAt,
        DelayMinutes = trip.DelayMinutes,
        LastObservedAt = trip.LastObservedAt,
    };

    public Trip ToTrip(TripId id) =>
        new(id, StopId, Line, Direction, PlannedAt, DelayMinutes, LastObservedAt);
}
