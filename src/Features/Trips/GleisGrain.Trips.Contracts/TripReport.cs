namespace GleisGrain.Trips.Contracts;

/// <summary>What a stop saw of a trip at one refresh.</summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Trips.Contracts.TripReport")]
public sealed record TripReport(
    [property: Id(0)] string StopId,
    [property: Id(1)] string Line,
    [property: Id(2)] string Direction,
    [property: Id(3)] DateTimeOffset PlannedAt,
    [property: Id(4)] int DelayMinutes,
    [property: Id(5)] DateTimeOffset ObservedAt);
