namespace GleisGrain.Ingestion.Domain;

/// <summary>Keeps a minimum interval between outgoing calls. Pure: the caller supplies the time and does the waiting.</summary>
internal sealed class RateLimiter(TimeSpan minimumInterval)
{
    private DateTimeOffset? _lastCallAt;

    /// <summary>Books the next call and returns how long to wait before making it.</summary>
    public TimeSpan Reserve(DateTimeOffset now)
    {
        var earliest = _lastCallAt + minimumInterval;
        var wait = earliest > now ? earliest.Value - now : TimeSpan.Zero;
        _lastCallAt = now + wait;

        return wait;
    }
}
