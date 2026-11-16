using GleisGrain.Common.Options;

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
    public const string MeterName = "GleisGrain.Stops";

    public static TBuilder AddStopsModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<StopsOptions>(StopsOptions.SectionName);

        return builder;
    }

    public static IEndpointRouteBuilder MapStopsEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/stops");

        return routes;
    }
}
