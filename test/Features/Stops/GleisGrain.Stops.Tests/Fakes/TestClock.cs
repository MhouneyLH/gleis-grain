using Microsoft.Extensions.Time.Testing;

namespace GleisGrain.Stops.Tests.Fakes;

/// <summary>
/// The time provider of the test silo. Only "now" comes from the fake clock. Timers and delays stay on the system clock,
/// because Orleans uses the same provider for its own timeouts and retries and would hang on a frozen clock.
/// </summary>
public sealed class TestClock(FakeTimeProvider time) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => time.GetUtcNow();
}
