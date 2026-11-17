using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using GleisGrain.Common;
using GleisGrain.Ingestion.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Tests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GleisGrain.Stops.Tests;

/// <summary>Runs the Stops module in a web application with its own silo, like the host does.</summary>
public class StopEndpointTests
{
    private const string Hbf = "60501720";

    [Fact]
    public async Task GetStops_ListsTheCatalogueWithoutActivatingAnyStopGrain()
    {
        await using var host = await StartAsync();

        var stops = await host.Client.GetFromJsonAsync<JsonElement>("/api/stops", TestContext.Current.CancellationToken);

        Assert.Equal(25, stops.GetArrayLength());
        var first = stops[0];
        Assert.Equal(Hbf, first.GetProperty("id").GetString());
        Assert.Equal("Hauptbahnhof", first.GetProperty("name").GetString());
        Assert.True(first.GetProperty("latitude").GetDouble() > 48);
        Assert.True(first.GetProperty("longitude").GetDouble() > 14);
        var activations = await host.Grains.GetGrain<IManagementGrain>(0).GetSimpleGrainStatistics(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(activations, activation => activation.GrainType.Contains("stop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetStop_CuratedStop_ReturnsTheBoard()
    {
        await using var host = await StartAsync();
        var plannedAt = host.Fakes.Time.GetUtcNow().AddMinutes(5);
        host.Fakes.SetDepartures(Hbf, new DepartureSnapshot("1", "Universität", plannedAt, 2, "A"));

        var board = await host.Client.GetFromJsonAsync<JsonElement>($"/api/stops/{Hbf}", TestContext.Current.CancellationToken);

        Assert.Equal("Hauptbahnhof", board.GetProperty("name").GetString());
        Assert.Equal(0, board.GetProperty("watchers").GetInt32());
        var departure = Assert.Single(board.GetProperty("departures").EnumerateArray());
        Assert.Equal($"{Hbf}:1:{plannedAt:yyyyMMddHHmm}", departure.GetProperty("tripId").GetString());
        Assert.Equal("Universität", departure.GetProperty("direction").GetString());
        Assert.Equal(2, departure.GetProperty("delayMinutes").GetInt32());
        Assert.Equal("A", departure.GetProperty("platform").GetString());
    }

    [Fact]
    public async Task GetStop_UnknownStop_Returns404WithoutActivatingAGrain()
    {
        await using var host = await StartAsync();

        var response = await host.Client.GetAsync("/api/stops/nowhere", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("stops.stop.not_found", problem.GetProperty("code").GetString());
        var activations = await host.Grains.GetGrain<IManagementGrain>(0).GetSimpleGrainStatistics(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(activations, activation => activation.GrainType.Contains("stop", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetStop_WithoutBoardCache_ShowsEveryChangeOfTheStopGrain()
    {
        await using var host = await StartAsync();
        await host.Client.GetAsync($"/api/stops/{Hbf}", TestContext.Current.CancellationToken);

        await host.Grains.GetGrain<IStopGrain>(Hbf).WatchAsync("Max");
        var board = await host.Client.GetFromJsonAsync<JsonElement>($"/api/stops/{Hbf}", TestContext.Current.CancellationToken);

        Assert.Equal(1, board.GetProperty("watchers").GetInt32());
    }

    [Fact]
    public async Task GetStop_WithBoardCache_ServesTheCachedSnapshot()
    {
        await using var host = await StartAsync(new() { ["Stops:UseBoardCache"] = "true", ["Stops:BoardCacheTtl"] = "00:10:00" });
        await host.Client.GetAsync($"/api/stops/{Hbf}", TestContext.Current.CancellationToken);

        await host.Grains.GetGrain<IStopGrain>(Hbf).WatchAsync("Max");
        var board = await host.Client.GetFromJsonAsync<JsonElement>($"/api/stops/{Hbf}", TestContext.Current.CancellationToken);

        Assert.Equal(0, board.GetProperty("watchers").GetInt32());
    }

    private static async Task<ApiHost> StartAsync(Dictionary<string, string?>? settings = null)
    {
        var fakes = new StopsFakes();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{FreePort()}");
        builder.Configuration.AddInMemoryCollection(settings ?? []);
        builder.Services.AddSingleton(fakes);
        builder.Services.AddSingleton<TimeProvider>(new TestClock(fakes.Time));
        builder.AddGleisGrainHttp();
        builder.AddStopsModule();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: FreePort());
            silo.AddMemoryGrainStorage(StopsModule.StorageName);
            silo.UseInMemoryReminderService();
        });

        var app = builder.Build();
        app.UseGleisGrainHttp();
        app.MapGroup("/api").MapStopsEndpoints();
        await app.StartAsync();

        return new ApiHost(app, fakes, new HttpClient { BaseAddress = new Uri(app.Urls.First()) });
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class ApiHost(WebApplication app, StopsFakes fakes, HttpClient client) : IAsyncDisposable
    {
        public StopsFakes Fakes { get; } = fakes;

        public HttpClient Client { get; } = client;

        public IGrainFactory Grains => app.Services.GetRequiredService<IGrainFactory>();

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
