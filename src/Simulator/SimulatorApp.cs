using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GleisGrain.Simulator;

/// <summary>Composes the simulator: the background simulation plus the control API. The tests build the same app with a fake downstream.</summary>
public static class SimulatorApp
{
    public static WebApplication Build(string[] args, Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        // One log line per request would drown the one-line summary.
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

        builder.Services.AddOptions<SimulatorOptions>()
            .BindConfiguration(SimulatorOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(provider =>
            provider.GetRequiredService<IOptions<SimulatorOptions>>().Value.Seed is { } seed ? new Random(seed) : new Random());
        builder.Services.AddHttpClient<GleisGrainClient>((provider, http) =>
        {
            http.BaseAddress = new Uri(provider.GetRequiredService<IOptions<SimulatorOptions>>().Value.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddSingleton<AudienceSimulator>();
        builder.Services.AddHostedService<SimulatorWorker>();
        builder.Services.AddProblemDetails();
        configureServices?.Invoke(builder.Services);

        // A fixed port: the frontend dev server and simulator.http call it. Port 0 lets the system choose (tests).
        var port = builder.Configuration.GetValue($"{SimulatorOptions.SectionName}:Port", 5090);
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var app = builder.Build();
        app.UseStatusCodePages();
        app.MapScenario();
        return app;
    }
}
