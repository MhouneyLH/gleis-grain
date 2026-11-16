using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;

namespace GleisGrain.Common.Tests;

/// <summary>What reaches an OpenTelemetry exporter (the Aspire dashboard is one) when the host is wired with AddGleisGrainObservability.</summary>
public sealed partial class ObservabilityExtensionsTests
{
    private const string FeatureMeter = "GleisGrain.Tests.Feature";

    [Fact]
    public async Task Logs_AreExportedWithTemplateAndStructuredAttributes()
    {
        var logs = new List<LogRecord>();
        using var host = await StartAsync(logs: logs);

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Tests");
        LogRefreshed(logger, "60501720", 8);

        var record = Assert.Single(logs, log => log.CategoryName == "Tests");
        var attributes = record.Attributes!.ToDictionary(a => a.Key, a => a.Value);
        Assert.Equal("60501720", attributes["StopId"]);
        Assert.Equal(8, attributes["Departures"]);
        Assert.Equal("Stop {StopId} refreshed with {Departures} departures", record.Body);
        Assert.Equal("Stop 60501720 refreshed with 8 departures", record.FormattedMessage);
    }

    [Fact]
    public async Task Metrics_OfRegisteredFeatureMeters_AreExported()
    {
        var metrics = new List<Metric>();
        using var host = await StartAsync(metrics: metrics);
        using var registered = new Meter(FeatureMeter);
        using var other = new Meter("GleisGrain.Tests.NotRegistered");

        registered.CreateCounter<long>("gleisgrain.tests.registered").Add(1);
        other.CreateCounter<long>("gleisgrain.tests.other").Add(1);
        host.Services.GetRequiredService<MeterProvider>().ForceFlush();

        Assert.Contains(metrics, metric => metric.Name == "gleisgrain.tests.registered");
        Assert.DoesNotContain(metrics, metric => metric.Name == "gleisgrain.tests.other");
    }

    [Fact]
    public async Task ServiceName_WhenTheEnvironmentNamesTheService_ComesFromTheEnvironment()
    {
        // Aspire names every resource through OTEL_SERVICE_NAME; the dashboard groups the replicas by that name.
        var previous = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "gleis-grain");
        try
        {
            using var host = await StartAsync();

            var resource = host.Services.GetRequiredService<MeterProvider>().GetResource();
            Assert.Equal("gleis-grain", resource.Attributes.Single(a => a.Key == "service.name").Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", previous);
        }
    }

    [Fact]
    public async Task ServiceName_WithoutEnvironment_UsesTheGivenName()
    {
        var previous = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
        Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", null);
        try
        {
            using var host = await StartAsync();

            var resource = host.Services.GetRequiredService<MeterProvider>().GetResource();
            Assert.Equal("gleisgrain-test", resource.Attributes.Single(a => a.Key == "service.name").Value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", previous);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stop {StopId} refreshed with {Departures} departures")]
    private static partial void LogRefreshed(ILogger logger, string stopId, int departures);

    private static async Task<IHost> StartAsync(List<LogRecord>? logs = null, List<Metric>? metrics = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddGleisGrainObservability("gleisgrain-test", FeatureMeter);

        if (logs is not null)
        {
            builder.Logging.AddOpenTelemetry(options => options.AddInMemoryExporter(logs));
        }

        if (metrics is not null)
        {
            builder.Services.ConfigureOpenTelemetryMeterProvider(provider => provider.AddInMemoryExporter(metrics));
        }
        else
        {
            builder.Services.ConfigureOpenTelemetryMeterProvider(provider => provider.AddInMemoryExporter(new List<Metric>()));
        }

        var host = builder.Build();
        await host.StartAsync();

        return host;
    }
}
