namespace GleisGrain.Stops.Contracts;

/// <summary>A stop of the curated Linz set: its departure board and the people watching it.</summary>
/// <remarks>The grain key is the <see cref="StopId"/> value, which is also the stop id of the EFA server.</remarks>
[Alias("GleisGrain.Stops.Contracts.IStopGrain")]
public interface IStopGrain : IGrainWithStringKey
{
    /// <summary>The board, or null when the id is not part of the curated set.</summary>
    [Alias("GetBoardAsync")]
    Task<StopBoardSnapshot?> GetBoardAsync();

    /// <summary>Adds a watcher. Idempotent per watcher. Returns the new watcher count.</summary>
    [Alias("WatchAsync")]
    Task<int> WatchAsync(string watcher);

    /// <summary>Removes a watcher. Idempotent per watcher. Returns the new watcher count.</summary>
    [Alias("UnwatchAsync")]
    Task<int> UnwatchAsync(string watcher);
}
