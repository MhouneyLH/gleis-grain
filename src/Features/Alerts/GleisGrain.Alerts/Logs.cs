namespace GleisGrain.Alerts;

internal static partial class AlertsLogs
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Debug, Message = "Looking up alerts for line {Line} and stop {StopId}")]
    public static partial void LookingUpAlerts(this ILogger logger, string? line, string? stopId);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Debug, Message = "Alert board refreshed: {Count} alerts, changed: {Changed}")]
    public static partial void BoardRefreshed(this ILogger logger, int count, bool changed);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning, Message = "Refreshing the alert board failed, keeping the alerts we have")]
    public static partial void RefreshFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning, Message = "The service notices cannot be loaded and the board has none")]
    public static partial void Unavailable(this ILogger logger, Exception exception);
}
