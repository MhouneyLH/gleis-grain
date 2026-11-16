using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Contracts;

[GenerateSerializer, Immutable, Alias("GleisGrain.Game.Contracts.PlayerSnapshot")]
public sealed record PlayerSnapshot(
    [property: Id(0)] Nickname Nickname,
    [property: Id(1)] int Score,
    [property: Id(2)] StopId? WatchedStop,
    [property: Id(3)] OpenBet? OpenBet);

[GenerateSerializer, Immutable, Alias("GleisGrain.Game.Contracts.OpenBet")]
public sealed record OpenBet(
    [property: Id(0)] TripId TripId,
    [property: Id(1)] string StopId,
    [property: Id(2)] string Line,
    [property: Id(3)] DateTimeOffset PlannedAt,
    [property: Id(4)] int GuessedDelayMinutes);

/// <summary>The result of a resolved bet. <see cref="Points"/> is 3 for an exact guess, 1 for off by one minute, otherwise 0.</summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Game.Contracts.BetResolution")]
public sealed record BetResolution(
    [property: Id(0)] TripId TripId,
    [property: Id(1)] int GuessedDelayMinutes,
    [property: Id(2)] int ActualDelayMinutes,
    [property: Id(3)] int Points);
