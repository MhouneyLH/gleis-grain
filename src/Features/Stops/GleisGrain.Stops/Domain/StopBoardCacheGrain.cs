using GleisGrain.Stops.Contracts;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace GleisGrain.Stops.Domain;

/// <summary>
/// The fix for one hot Hauptbahnhof grain. A stop grain handles one call at a time, so when the whole room
/// reads the same board, every request queues behind the others. A stateless worker has several activations
/// per silo, each keeps the last snapshot for a few seconds and only then asks the stop grain again.
/// The stop grain sees a handful of calls instead of one per viewer. Watching still goes straight to the stop grain.
/// </summary>
[StatelessWorker, GrainType("stop-board-cache")]
internal sealed class StopBoardCacheGrain(
    IOptions<StopsOptions> options,
    TimeProvider time,
    StopsMetrics metrics)
    : Grain, IStopBoardCacheGrain
{
    private StopBoardSnapshot? _snapshot;
    private DateTimeOffset _cachedAt;

    public async Task<StopBoardSnapshot?> GetBoardAsync()
    {
        if (_snapshot is not null && time.GetUtcNow() - _cachedAt < options.Value.BoardCacheTtl)
        {
            metrics.BoardCacheHit();

            return _snapshot;
        }

        // Unknown stops answer null and are not cached.
        _snapshot = await GrainFactory.GetGrain<IStopGrain>(this.GetPrimaryKeyString()).GetBoardAsync();
        _cachedAt = time.GetUtcNow();

        return _snapshot;
    }
}
