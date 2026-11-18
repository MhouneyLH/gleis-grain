namespace GleisGrain.Cluster.Contracts;

// The payloads of every live event. The JSON uses camelCase property names. The frontend mirrors them in src/Frontend/src/api.

/// <summary>A stop grain woke up on a silo.</summary>
public sealed record StopActivated(string StopId, string Silo) : ILivePayload
{
    public static string EventType => "stop.activated";
}

/// <summary>A stop grain was deactivated because nobody used it.</summary>
public sealed record StopDeactivated(string StopId) : ILivePayload
{
    public static string EventType => "stop.deactivated";
}

public sealed record StopWatchersChanged(string StopId, int Watchers) : ILivePayload
{
    public static string EventType => "stop.watchers";
}
