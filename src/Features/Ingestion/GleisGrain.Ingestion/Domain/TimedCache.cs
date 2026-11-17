namespace GleisGrain.Ingestion.Domain;

/// <summary>
/// Values that stay fresh for a time to live. A value past its time is no longer fresh but stays
/// readable as stale, so callers can fall back to it when a refresh fails.
/// </summary>
internal sealed class TimedCache<TKey, TValue>(TimeSpan timeToLive)
    where TKey : notnull
{
    private readonly Dictionary<TKey, (TValue Value, DateTimeOffset StoredAt)> _entries = [];

    public void Set(TKey key, TValue value, DateTimeOffset now) => _entries[key] = (value, now);

    public bool TryGetFresh(TKey key, DateTimeOffset now, out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry) && now - entry.StoredAt < timeToLive)
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }

    public bool TryGetStale(TKey key, out TValue value)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }
}
