namespace GleisGrain.Trips.Contracts;

/// <summary>
/// A trip with its latest known delay. <see cref="HasDeparted"/> is true once planned time plus delay has passed;
/// from then on <see cref="DelayMinutes"/> is the final delay.
/// </summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Trips.Contracts.TripSnapshot")]
public sealed record TripSnapshot(
    [property: Id(0)] TripId Id,
    [property: Id(1)] string StopId,
    [property: Id(2)] string Line,
    [property: Id(3)] string Direction,
    [property: Id(4)] DateTimeOffset PlannedAt,
    [property: Id(5)] int DelayMinutes,
    [property: Id(6)] bool HasDeparted);
