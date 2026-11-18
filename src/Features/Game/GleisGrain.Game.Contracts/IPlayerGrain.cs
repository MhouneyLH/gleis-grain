using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Contracts;

/// <summary>One person: nickname and watched stop.</summary>
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
}

[GenerateSerializer, Alias("GleisGrain.Game.Contracts.WatchStopOutcome")]
public enum WatchStopOutcome
{
    Watching = 0,
    NoPlayer = 1,
    UnknownStop = 2,
}
