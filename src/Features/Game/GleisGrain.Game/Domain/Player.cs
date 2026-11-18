using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>
/// Pure domain model: no Orleans runtime, no I/O. A person with a nickname and a watched stop.
/// The grain loads it from its state, calls it and persists the result.
/// </summary>
internal sealed class Player(Nickname nickname, StopId? watchedStop = null)
{
    public Nickname Nickname { get; } = nickname;

    public StopId? WatchedStop { get; private set; } = watchedStop;

    public void Watch(StopId stopId) => WatchedStop = stopId;

    public PlayerSnapshot ToSnapshot() => new(Nickname, WatchedStop);
}
