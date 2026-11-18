using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game;

internal static partial class GameLogs
{
    [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "Player {Nickname} joined")]
    public static partial void PlayerClaimed(this ILogger logger, Nickname nickname);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Debug, Message = "Player {Nickname} watches stop {StopId}")]
    public static partial void PlayerWatchesStop(this ILogger logger, Nickname nickname, StopId stopId);
}
