using System.ComponentModel;
using GleisGrain.Ingestion.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GleisGrain.Cluster.Endpoints;

internal sealed record IngestionStatsResponse
{
    [Description("Requests that went out to Linz AG.")]
    public required long Calls { get; init; }

    [Description("Requests the cache answered.")]
    public required long CacheHits { get; init; }
}

internal sealed record GetStatsResponse
{
    public required IngestionStatsResponse Ingestion { get; init; }

    [Description("Number of active grains per grain type, summed over all silos.")]
    public required IReadOnlyDictionary<string, int> ActiveGrains { get; init; }
}

internal sealed class GetStats
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/stats", Handle)
            .WithName("GetStats")
            .WithSummary("Shows cache hits against Linz AG calls and the active grains.")
            .WithDescription("Part of the Cluster context: feeds the stats panel.");

    private static async Task<Ok<GetStatsResponse>> Handle(IGrainFactory grains)
    {
        var source = await grains.GetGrain<ISourceGrain>(SourceId.LinzAg.Value).GetStatsAsync();
        var statistics = await grains.GetGrain<IManagementGrain>(0).GetSimpleGrainStatistics();

        return TypedResults.Ok(new GetStatsResponse
        {
            Ingestion = new IngestionStatsResponse { Calls = source.Calls, CacheHits = source.CacheHits },
            ActiveGrains = statistics
                .GroupBy(statistic => ShortName(statistic.GrainType), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Sum(statistic => statistic.ActivationCount), StringComparer.Ordinal),
        });
    }

    // The statistics name the grain class as "Namespace.Class,Assembly". The panel only needs "Class".
    private static string ShortName(string grainClass)
    {
        var name = grainClass.AsSpan();
        name = name[..(name.IndexOf(',') is var comma and >= 0 ? comma : name.Length)];

        return name[(name.LastIndexOf('.') + 1)..].ToString();
    }
}
