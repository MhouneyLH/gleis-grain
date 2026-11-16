namespace GleisGrain.Cluster.Contracts;

// The payloads of every live event. The JSON uses camelCase property names. docs/api.md lists them for the frontend.

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

public sealed record BetPlaced(string Nickname, string StopId, string Line) : ILivePayload
{
    public static string EventType => "bet.placed";
}

public sealed record BetResolved(string Nickname, string TripId, int ActualDelayMinutes, int Points) : ILivePayload
{
    public static string EventType => "bet.resolved";
}

public sealed record LeaderboardRow(string Nickname, int Score);

public sealed record LeaderboardChanged(IReadOnlyList<LeaderboardRow> Top) : ILivePayload
{
    public static string EventType => "leaderboard.changed";
}
