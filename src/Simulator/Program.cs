using GleisGrain.Simulator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

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

await builder.Build().RunAsync();
