using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game;

internal static partial class GameLogs
{
    [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "Player {Nickname} joined")]
    public static partial void PlayerClaimed(this ILogger logger, Nickname nickname);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Debug, Message = "Player {Nickname} watches stop {StopId}")]
    public static partial void PlayerWatchesStop(this ILogger logger, Nickname nickname, StopId stopId);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Player {Nickname} bets {Guess} minutes on trip {TripId}")]
    public static partial void BetPlaced(this ILogger logger, Nickname nickname, int guess, TripId tripId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Player {Nickname} earned {Points} points on trip {TripId}")]
    public static partial void BetResolved(this ILogger logger, Nickname nickname, int points, TripId tripId);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Debug, Message = "Leaderboard changed, {Players} players")]
    public static partial void LeaderboardChanged(this ILogger logger, int players);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Warning, Message = "Could not hand over {Call} to another grain")]
    public static partial void FireAndForgetFailed(this ILogger logger, Exception exception, string call);
}
