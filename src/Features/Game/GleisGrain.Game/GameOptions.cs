using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Game;

internal sealed class GameOptions
{
    public const string SectionName = "Game";

    /// <summary>Grain storage name; equals the connection string name of the game database.</summary>
    public const string StorageName = "game";

    /// <summary>
    /// How often a player's reminder looks for a bet whose departure has passed.
    /// Orleans does not accept reminder periods below one minute.
    /// </summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan ResolvePeriod { get; init; } = TimeSpan.FromMinutes(1);
}
