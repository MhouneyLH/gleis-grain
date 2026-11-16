using GleisGrain.Trips.Contracts;

namespace GleisGrain.Trips;

internal static partial class TripsLogs
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug, Message = "Looking up trip {TripId}")]
    public static partial void LookingUpTrip(this ILogger logger, TripId tripId);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "Trip {TripId} started by its first report")]
    public static partial void TripStarted(this ILogger logger, TripId tripId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Debug, Message = "Report for trip {TripId} handled, delay changed: {DelayChanged}")]
    public static partial void ReportHandled(this ILogger logger, TripId tripId, bool delayChanged);
}
