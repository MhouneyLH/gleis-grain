using GleisGrain.Common.Options;

namespace GleisGrain.Cluster;

/// <summary>
/// Public surface of the Cluster module. Everything else in this project is internal.
/// The host calls <see cref="AddClusterModule{TBuilder}"/> and <see cref="MapClusterEndpoints"/>.
/// The module keeps no state, so it has no storage name.
/// </summary>
public static class ClusterModule
{
    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = "GleisGrain.Cluster";

    public static TBuilder AddClusterModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<ClusterOptions>(ClusterOptions.SectionName);

        return builder;
    }

    public static IEndpointRouteBuilder MapClusterEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/cluster");

        return routes;
    }
}
