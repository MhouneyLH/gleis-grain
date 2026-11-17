using GleisGrain.Ingestion.Domain;

namespace GleisGrain.Ingestion.Tests;

public class RateLimiterTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reserve_FirstCall_DoesNotWait() =>
        Assert.Equal(TimeSpan.Zero, new RateLimiter(TimeSpan.FromMilliseconds(500)).Reserve(_start));

    [Fact]
    public void Reserve_CallRightAfterAnother_WaitsTheRestOfTheInterval()
    {
        var limiter = new RateLimiter(TimeSpan.FromMilliseconds(500));
        limiter.Reserve(_start);

        Assert.Equal(TimeSpan.FromMilliseconds(300), limiter.Reserve(_start + TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void Reserve_CallAfterTheInterval_DoesNotWait()
    {
        var limiter = new RateLimiter(TimeSpan.FromMilliseconds(500));
        limiter.Reserve(_start);

        Assert.Equal(TimeSpan.Zero, limiter.Reserve(_start + TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public void Reserve_CallsAtTheSameTime_QueueUpOneIntervalApart()
    {
        var limiter = new RateLimiter(TimeSpan.FromMilliseconds(500));

        var waits = Enumerable.Range(0, 3).Select(_ => limiter.Reserve(_start)).ToList();

        Assert.Equal([TimeSpan.Zero, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1000)], waits);
    }

    [Fact]
    public void Reserve_ZeroInterval_NeverWaits()
    {
        var limiter = new RateLimiter(TimeSpan.Zero);

        Assert.Equal(TimeSpan.Zero, limiter.Reserve(_start));
        Assert.Equal(TimeSpan.Zero, limiter.Reserve(_start));
    }
}
