using GleisGrain.Cluster.Endpoints;
using GleisGrain.Cluster.Events;
using GleisGrain.Common.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GleisGrain.Cluster;

/// <summary>
/// Public surface of the Cluster module. Everything else in this project is internal.
/// The host calls <see cref="AddClusterModule{TBuilder}"/> and <see cref="MapClusterEndpoints"/>.
/// The module keeps no state, so it has no storage name.
/// </summary>
public static class ClusterModule
{
    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = ClusterMetrics.MeterName;

    public static TBuilder AddClusterModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddClusterServices();

        return builder;
    }

    public static IEndpointRouteBuilder MapClusterEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/cluster");

        GetSilos.Map(api);
        GetActivations.Map(api);
        GetStats.Map(api);
        KillSilo.Map(api);
        StreamEvents.Map(api);

        return routes;
    }

    // Separate from AddClusterModule so tests can add the module to a silo builder, which is no IHostApplicationBuilder.
    internal static IServiceCollection AddClusterServices(this IServiceCollection services)
    {
        services.AddValidatedOptions<ClusterOptions>(ClusterOptions.SectionName);
        services.AddMetrics();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ClusterMetrics>();
        services.AddSingleton<LiveEventBroadcaster>();
        services.AddSingleton<IProcessKiller, ProcessKiller>();
        services.AddHostedService<LiveEventRelay>();

        return services;
    }
}
