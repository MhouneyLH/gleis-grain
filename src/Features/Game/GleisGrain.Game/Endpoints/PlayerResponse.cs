using System.ComponentModel;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Endpoints;

/// <summary>The player as every game endpoint returns it.</summary>
internal sealed record PlayerResponse
{
    [Description("The nickname as it was claimed.")]
    public required Nickname Nickname { get; init; }

    [Description("The stop the player watches, null when none.")]
    public required StopId? WatchedStopId { get; init; }

    public static PlayerResponse From(PlayerSnapshot player) => new()
    {
        Nickname = player.Nickname,
        WatchedStopId = player.WatchedStop,
    };
}
