using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops;

internal static partial class StopsLogs
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Debug, Message = "Looking up the board of stop {StopId}")]
    public static partial void LookingUpBoard(this ILogger logger, StopId stopId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Refreshed stop {StopId} ({Trigger}): {Departures} departures")]
    public static partial void BoardRefreshed(this ILogger logger, string stopId, string trigger, int departures);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Refreshing stop {StopId} ({Trigger}) failed, the old board stays")]
    public static partial void RefreshFailed(this ILogger logger, Exception exception, string stopId, string trigger);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Publishing a live event for stop {StopId} failed")]
    public static partial void PublishFailed(this ILogger logger, Exception exception, string stopId);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Debug, Message = "Stop {StopId} now has {Watchers} watchers")]
    public static partial void WatchersChanged(this ILogger logger, string stopId, int watchers);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Debug, Message = "Grain {GrainKey} is not a curated stop")]
    public static partial void UnknownStop(this ILogger logger, string grainKey);
}
