using GleisGrain.Ingestion.Domain;

namespace GleisGrain.Ingestion.Tests;

public class TimedCacheTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryGetFresh_UnknownKey_ReturnsFalse() =>
        Assert.False(new TimedCache<string, int>(TimeSpan.FromSeconds(20)).TryGetFresh("a", _start, out _));

    [Fact]
    public void TryGetFresh_WithinTimeToLive_ReturnsTheValue()
    {
        var cache = new TimedCache<string, int>(TimeSpan.FromSeconds(20));
        cache.Set("a", 7, _start);

        Assert.True(cache.TryGetFresh("a", _start + TimeSpan.FromSeconds(19), out var value));
        Assert.Equal(7, value);
    }

    [Fact]
    public void TryGetFresh_AtTimeToLive_ReturnsFalse()
    {
        var cache = new TimedCache<string, int>(TimeSpan.FromSeconds(20));
        cache.Set("a", 7, _start);

        Assert.False(cache.TryGetFresh("a", _start + TimeSpan.FromSeconds(20), out _));
    }

    [Fact]
    public void TryGetStale_ExpiredValue_IsStillAvailable()
    {
        var cache = new TimedCache<string, int>(TimeSpan.FromSeconds(20));
        cache.Set("a", 7, _start);

        Assert.True(cache.TryGetStale("a", out var value));
        Assert.Equal(7, value);
    }

    [Fact]
    public void Set_ExistingKey_ReplacesValueAndRestartsTheClock()
    {
        var cache = new TimedCache<string, int>(TimeSpan.FromSeconds(20));
        cache.Set("a", 1, _start);
        cache.Set("a", 2, _start + TimeSpan.FromSeconds(30));

        Assert.True(cache.TryGetFresh("a", _start + TimeSpan.FromSeconds(40), out var value));
        Assert.Equal(2, value);
    }

    [Fact]
    public void TryGetFresh_OtherKey_IsIndependent()
    {
        var cache = new TimedCache<string, int>(TimeSpan.FromSeconds(20));
        cache.Set("a", 1, _start);

        Assert.False(cache.TryGetFresh("b", _start, out _));
    }
}
