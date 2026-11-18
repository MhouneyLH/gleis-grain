using GleisGrain.Common.Options;
using GleisGrain.Stops.Catalogue;
using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GleisGrain.Stops;

/// <summary>
/// Public surface of the Stops module. Everything else in this project is internal.
/// The host calls <see cref="AddStopsModule{TBuilder}"/> and <see cref="MapStopsEndpoints"/>.
/// </summary>
public static class StopsModule
{
    /// <summary>Grain storage name, equals the connection string name of the stops database.</summary>
    public const string StorageName = StopsOptions.StorageName;

    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = StopsMetrics.MeterName;

    /// <summary>The grain interface version of this build (1 by default, 2 with <c>-p:GrainVersion=2</c>).</summary>
    public const int GrainVersion = StopsGrainVersion.Current;

    public static TBuilder AddStopsModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<StopsOptions>(StopsOptions.SectionName);
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(StopCatalogue.Load());
        builder.Services.AddSingleton<StopsMetrics>();

        return builder;
    }

    public static IEndpointRouteBuilder MapStopsEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/stops");
        GetStops.Map(api);
        GetStop.Map(api);

        return routes;
    }
}
