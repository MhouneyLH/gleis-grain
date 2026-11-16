using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Contracts;

/// <summary>One person: nickname, score, watched stop and at most one open bet.</summary>
/// <remarks>The grain key is <see cref="Nickname.Key"/>.</remarks>
[Alias("GleisGrain.Game.Contracts.IPlayerGrain")]
public interface IPlayerGrain : IGrainWithStringKey
{
    /// <summary>Takes the nickname. False when somebody already has it.</summary>
    [Alias("ClaimAsync")]
    Task<bool> ClaimAsync(Nickname nickname);

    /// <summary>The player, or null when the nickname was never claimed.</summary>
    [Alias("GetAsync")]
    Task<PlayerSnapshot?> GetAsync();

    /// <summary>Moves the player to a stop: leaves the old one and becomes a watcher of the new one.</summary>
    [Alias("WatchStopAsync")]
    Task<WatchStopOutcome> WatchStopAsync(StopId stopId);

    [Alias("PlaceBetAsync")]
    Task<PlaceBetOutcome> PlaceBetAsync(TripId tripId, int guessedDelayMinutes);

    /// <summary>Resolves the open bet when its departure has passed. The reminder calls this; it is also public so tests and the presenter can trigger it.</summary>
    [Alias("ResolveBetAsync")]
    Task<BetResolution?> ResolveBetAsync();
}

[GenerateSerializer, Alias("GleisGrain.Game.Contracts.WatchStopOutcome")]
public enum WatchStopOutcome
{
    Watching = 0,
    NoPlayer = 1,
    UnknownStop = 2,
}

[GenerateSerializer, Alias("GleisGrain.Game.Contracts.PlaceBetOutcome")]
public enum PlaceBetOutcome
{
    Placed = 0,
    NoPlayer = 1,
    AlreadyHasOpenBet = 2,
    UnknownTrip = 3,
    TripAlreadyDeparted = 4,
    InvalidGuess = 5,
}
