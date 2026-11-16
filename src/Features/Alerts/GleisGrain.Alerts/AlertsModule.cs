using GleisGrain.Common.Options;

namespace GleisGrain.Alerts;

/// <summary>
/// Public surface of the Alerts module. Everything else in this project is internal.
/// The host calls <see cref="AddAlertsModule{TBuilder}"/> and <see cref="MapAlertsEndpoints"/>.
/// </summary>
public static class AlertsModule
{
    /// <summary>Grain storage name, equals the connection string name of the alerts database.</summary>
    public const string StorageName = AlertsOptions.StorageName;

    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = "GleisGrain.Alerts";

    public static TBuilder AddAlertsModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<AlertsOptions>(AlertsOptions.SectionName);

        return builder;
    }

    public static IEndpointRouteBuilder MapAlertsEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/alerts");

        return routes;
    }
}
