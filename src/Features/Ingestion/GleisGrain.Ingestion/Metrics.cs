using System.Diagnostics.Metrics;

namespace GleisGrain.Ingestion;

internal sealed class IngestionMetrics
{
    public const string MeterName = "GleisGrain.Ingestion";

    private readonly Counter<long> _calls;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _failures;

    public IngestionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _calls = meter.CreateCounter<long>(
            "gleisgrain.ingestion.calls",
            unit: "{call}",
            description: "Number of calls sent to the EFA server, tagged by what was asked for.");
        _cacheHits = meter.CreateCounter<long>(
            "gleisgrain.ingestion.cache_hits",
            unit: "{hit}",
            description: "Number of requests answered from the cache, tagged by what was asked for.");
        _failures = meter.CreateCounter<long>(
            "gleisgrain.ingestion.failures",
            unit: "{call}",
            description: "Number of calls to the EFA server that failed, tagged by what was asked for.");
    }

    public void Called(string kind) => _calls.Add(1, new KeyValuePair<string, object?>("kind", kind));

    public void CacheHit(string kind) => _cacheHits.Add(1, new KeyValuePair<string, object?>("kind", kind));

    public void Failed(string kind) => _failures.Add(1, new KeyValuePair<string, object?>("kind", kind));
}
