namespace GleisGrain.Ingestion.Tests;

/// <summary>
/// The time provider of the test silo: the system clock plus an offset that tests move forward with <see cref="Advance"/>.
/// Only "now" is shifted. Timers and timestamps stay real, and the clock never starts in the past or the future, because
/// Orleans uses the same provider for its membership protocol and declares a silo dead whose clock disagrees with the table.
/// </summary>
public sealed class ShiftableClock : TimeProvider
{
    private long _offsetTicks;

    public void Advance(TimeSpan delta) => Interlocked.Add(ref _offsetTicks, delta.Ticks);

    public override DateTimeOffset GetUtcNow() => base.GetUtcNow() + TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));
}
