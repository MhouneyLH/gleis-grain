using GleisGrain.Trips.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GleisGrain.Trips;

/// <summary>
/// Public surface of the Trips module. Everything else in this project is internal.
/// The host calls <see cref="AddTripsModule{TBuilder}"/> and <see cref="MapTripsEndpoints"/>.
/// </summary>
public static class TripsModule
{
    /// <summary>Grain storage name, equals the connection string name of the trips database.</summary>
    public const string StorageName = TripsOptions.StorageName;

    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = TripsMetrics.MeterName;

    public static TBuilder AddTripsModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<TripsMetrics>();

        return builder;
    }

    public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/trips");
        GetTrip.Map(api);

        return routes;
    }
}
