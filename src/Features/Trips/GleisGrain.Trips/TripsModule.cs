using GleisGrain.Common.Options;

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
    public const string MeterName = "GleisGrain.Trips";

    public static TBuilder AddTripsModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<TripsOptions>(TripsOptions.SectionName);

        return builder;
    }

    public static IEndpointRouteBuilder MapTripsEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/trips");

        return routes;
    }
}
