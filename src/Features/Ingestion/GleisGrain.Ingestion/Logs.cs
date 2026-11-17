namespace GleisGrain.Ingestion;

internal static partial class IngestionLogs
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Debug, Message = "Calling EFA for {Kind}, waited {WaitMs} ms for the rate limit")]
    public static partial void CallingEfa(this ILogger logger, string kind, double waitMs);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "EFA call for {Kind} failed, serving the stale value")]
    public static partial void ServingStale(this ILogger logger, string kind, Exception exception);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "EFA call for {Kind} failed and there is nothing cached")]
    public static partial void CallFailed(this ILogger logger, string kind, Exception exception);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "No recorded departures for stop {StopId}, replaying an empty board")]
    public static partial void NoRecording(this ILogger logger, string stopId);
}
