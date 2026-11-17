using System.Diagnostics.Metrics;

namespace GleisGrain.Stops;

internal sealed class StopsMetrics
{
    public const string MeterName = "GleisGrain.Stops";

    private readonly Counter<long> _boardLookups;
    private readonly Counter<long> _boardRefreshes;
    private readonly Counter<long> _cacheHits;

    public StopsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _boardLookups = meter.CreateCounter<long>(
            "gleisgrain.stops.board.lookups",
            unit: "{lookup}",
            description: "Number of board requests, tagged by whether the stop was found.");
        _boardRefreshes = meter.CreateCounter<long>(
            "gleisgrain.stops.board.refreshes",
            unit: "{refresh}",
            description: "Number of board refreshes, tagged by trigger (request, reminder, timer) and success.");
        _cacheHits = meter.CreateCounter<long>(
            "gleisgrain.stops.board.cache_hits",
            unit: "{hit}",
            description: "Number of board requests answered by a stop board cache grain without asking the stop grain.");
    }

    public void BoardLookedUp(bool found) =>
        _boardLookups.Add(1, new KeyValuePair<string, object?>("found", found));

    public void BoardRefreshed(string trigger, bool succeeded) =>
        _boardRefreshes.Add(
            1,
            new KeyValuePair<string, object?>("trigger", trigger),
            new KeyValuePair<string, object?>("succeeded", succeeded));

    public void BoardCacheHit() => _cacheHits.Add(1);
}
