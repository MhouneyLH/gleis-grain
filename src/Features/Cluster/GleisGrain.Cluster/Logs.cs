namespace GleisGrain.Cluster;

internal static partial class ClusterLogs
{
    [LoggerMessage(EventId = 6000, Level = LogLevel.Information, Message = "Subscribed this silo to the cluster event hub, renewing every {Period}")]
    public static partial void SubscribedToHub(this ILogger logger, TimeSpan period);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Warning, Message = "Subscribing to the cluster event hub failed, retrying in {RetryIn}")]
    public static partial void SubscribeToHubFailed(this ILogger logger, Exception exception, TimeSpan retryIn);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Debug, Message = "Event client connected")]
    public static partial void EventClientConnected(this ILogger logger);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Debug, Message = "Event client disconnected")]
    public static partial void EventClientDisconnected(this ILogger logger);

    [LoggerMessage(EventId = 6004, Level = LogLevel.Warning, Message = "Chaos button pressed: this process is killed now")]
    public static partial void ChaosKillTriggered(this ILogger logger);
}
