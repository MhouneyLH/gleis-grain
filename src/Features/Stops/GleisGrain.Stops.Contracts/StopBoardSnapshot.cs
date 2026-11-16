using GleisGrain.Trips.Contracts;

namespace GleisGrain.Stops.Contracts;

[GenerateSerializer, Immutable, Alias("GleisGrain.Stops.Contracts.StopBoardSnapshot")]
public sealed record StopBoardSnapshot(
    [property: Id(0)] StopId Id,
    [property: Id(1)] string Name,
    [property: Id(2)] double Latitude,
    [property: Id(3)] double Longitude,
    [property: Id(4)] int Watchers,
    [property: Id(5)] DateTimeOffset? RefreshedAt,
    [property: Id(6)] IReadOnlyList<BoardDeparture> Departures);

[GenerateSerializer, Immutable, Alias("GleisGrain.Stops.Contracts.BoardDeparture")]
public sealed record BoardDeparture(
    [property: Id(0)] TripId TripId,
    [property: Id(1)] string Line,
    [property: Id(2)] string Direction,
    [property: Id(3)] DateTimeOffset PlannedAt,
    [property: Id(4)] int DelayMinutes,
    [property: Id(5)] string? Platform);
