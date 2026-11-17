using GleisGrain.Common.Options;
using GleisGrain.Ingestion.Feeds;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GleisGrain.Ingestion;

/// <summary>
/// Public surface of the Ingestion module. Everything else in this project is internal.
/// The host calls <see cref="AddIngestionModule{TBuilder}"/> and <see cref="MapIngestionEndpoints"/>.
/// </summary>
public static class IngestionModule
{
    /// <summary>Grain storage name, equals the connection string name of the ingestion database.</summary>
    public const string StorageName = IngestionOptions.StorageName;

    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = IngestionMetrics.MeterName;

    public static TBuilder AddIngestionModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<IngestionOptions>(IngestionOptions.SectionName);
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IngestionMetrics>();

        builder.Services
            .AddHttpClient<EfaHttpFeed>((services, client) =>
            {
                var options = services.GetRequiredService<IOptions<IngestionOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");
                client.Timeout = options.RequestTimeout;
                client.DefaultRequestHeaders.UserAgent.ParseAdd("gleis-grain-demo");
            });
        builder.Services.AddSingleton<ReplayFeed>();
        builder.Services.AddTransient<IEfaFeed>(services =>
            services.GetRequiredService<IOptions<IngestionOptions>>().Value.Mode == IngestionMode.Replay
                ? services.GetRequiredService<ReplayFeed>()
                : services.GetRequiredService<EfaHttpFeed>());

        return builder;
    }

    /// <summary>Ingestion has no HTTP endpoints; the group stays so every module is mapped the same way.</summary>
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder routes)
    {
        _ = routes.MapGroup("/ingestion");

        return routes;
    }
}
