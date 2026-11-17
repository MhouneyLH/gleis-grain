using GleisGrain.Ingestion.Contracts;
using GleisGrain.Ingestion.Feeds;
using Microsoft.Extensions.Options;

namespace GleisGrain.Ingestion.Domain;

/// <summary>
/// The one gateway to the EFA server. A grain handles one call at a time, so it is the rate limiter
/// for the whole cluster. Cache and counters live in memory on purpose: persisting every refresh
/// would cost a database write for data that is stale in seconds.
/// </summary>
internal sealed class SourceGrain : Grain, ISourceGrain
{
    private const string DeparturesKind = "departures";
    private const string NoticesKind = "notices";
    private const string NoticesKey = "all";

    private readonly IEfaFeed _feed;
    private readonly TimeProvider _timeProvider;
    private readonly IngestionMetrics _metrics;
    private readonly ILogger<SourceGrain> _logger;
    private readonly RateLimiter _rateLimiter;
    private readonly TimedCache<string, IReadOnlyList<DepartureSnapshot>> _departures;
    private readonly TimedCache<string, IReadOnlyList<NoticeSnapshot>> _notices;

    private long _calls;
    private long _cacheHits;
    private DateTimeOffset? _lastCallAt;

    public SourceGrain(IEfaFeed feed, IOptions<IngestionOptions> options, TimeProvider timeProvider, IngestionMetrics metrics, ILogger<SourceGrain> logger)
    {
        _feed = feed;
        _timeProvider = timeProvider;
        _metrics = metrics;
        _logger = logger;
        _rateLimiter = new RateLimiter(options.Value.MinimumCallInterval);
        _departures = new TimedCache<string, IReadOnlyList<DepartureSnapshot>>(options.Value.DepartureCacheTtl);
        _notices = new TimedCache<string, IReadOnlyList<NoticeSnapshot>>(options.Value.NoticeCacheTtl);
    }

    public Task<IReadOnlyList<DepartureSnapshot>> GetDeparturesAsync(string efaStopId) =>
        GetAsync(DeparturesKind, _departures, efaStopId, () => _feed.FetchDeparturesAsync(efaStopId, CancellationToken.None));

    public Task<IReadOnlyList<NoticeSnapshot>> GetNoticesAsync() =>
        GetAsync(NoticesKind, _notices, NoticesKey, () => _feed.FetchNoticesAsync(CancellationToken.None));

    public Task<SourceStats> GetStatsAsync() => Task.FromResult(new SourceStats(_calls, _cacheHits, _lastCallAt));

    private async Task<T> GetAsync<T>(string kind, TimedCache<string, T> cache, string key, Func<Task<T>> fetch)
    {
        if (cache.TryGetFresh(key, _timeProvider.GetUtcNow(), out var fresh))
        {
            _cacheHits++;
            _metrics.CacheHit(kind);
            return fresh;
        }

        try
        {
            var wait = _rateLimiter.Reserve(_timeProvider.GetUtcNow());
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, _timeProvider);
            }

            _calls++;
            _lastCallAt = _timeProvider.GetUtcNow();
            _metrics.Called(kind);
            _logger.CallingEfa(kind, wait.TotalMilliseconds);

            var value = await fetch();
            cache.Set(key, value, _timeProvider.GetUtcNow());

            return value;
        }
        catch (Exception exception)
        {
            _metrics.Failed(kind);

            if (!cache.TryGetStale(key, out var stale))
            {
                _logger.CallFailed(kind, exception);
                throw;
            }

            _logger.ServingStale(kind, exception);
            return stale;
        }
    }
}
