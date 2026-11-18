using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Contracts;

[GenerateSerializer, Immutable, Alias("GleisGrain.Game.Contracts.PlayerSnapshot")]
public sealed record PlayerSnapshot(
    [property: Id(0)] Nickname Nickname,
    [property: Id(2)] StopId? WatchedStop);
