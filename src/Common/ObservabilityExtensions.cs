using GleisGrain.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GleisGrain.Common;

public static class ObservabilityExtensions
{
    private const string OrleansMeter = "Microsoft.Orleans";
    private static readonly string[] _orleansActivitySources =
    [
        "Microsoft.Orleans.Runtime",
        "Microsoft.Orleans.Application",
    ];

    /// <summary>
    /// Wires OpenTelemetry logs, metrics and traces. The OTLP endpoint and the service name come from the standard
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> and <c>OTEL_SERVICE_NAME</c> variables, which the Aspire AppHost sets.
    /// </summary>
    public static TBuilder AddGleisGrainObservability<TBuilder>(this TBuilder builder, string serviceName, params string[] featureMeterNames)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<MetricsOptions>(MetricsOptions.SectionName);

        var metrics = builder.Configuration.GetSection(MetricsOptions.SectionName).Get<MetricsOptions>() ?? new MetricsOptions();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            // The environment goes last so it wins: Aspire names the resource and gives every replica its
            // own instance id through OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES. serviceName is the fallback.
            .ConfigureResource(resource => resource.AddService(serviceName).AddEnvironmentVariableDetector())
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(featureMeterNames);

                if (metrics.IncludeOrleansMeters)
                {
                    m.AddMeter(OrleansMeter);
                }

                m.AddOtlpExporter((_, reader) => reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = metrics.ExportIntervalMilliseconds);
            })
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(_orleansActivitySources)
                .AddOtlpExporter());

        return builder;
    }
}
