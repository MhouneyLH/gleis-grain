using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GleisGrain.Cluster.Tests.Support;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GleisGrain.Cluster.Tests;

/// <summary>The read endpoints against one real silo.</summary>
public sealed class ClusterEndpointTests : IAsyncLifetime
{
    private ClusterApp _app = null!;

    public async ValueTask InitializeAsync() => _app = await ClusterApp.StartAsync(enableChaos: true);

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task GetSilos_WithOneSilo_ListsItAsActive()
    {
        var json = await GetJsonAsync("/api/cluster/silos");

        var silo = Assert.Single(json.EnumerateArray());
        Assert.Equal("Active", silo.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(silo.GetProperty("name").GetString()));
        Assert.Contains("127.0.0.1", silo.GetProperty("address").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetActivations_AfterAStopGrainWasUsed_ListsItWithItsSilo()
    {
        await _app.Services.GetRequiredService<IGrainFactory>().GetGrain<IFakeStopGrain>("8f3a1c").PingAsync();

        var activations = await GetJsonAsync("/api/cluster/activations");
        var silos = await GetJsonAsync("/api/cluster/silos");

        var activation = Assert.Single(activations.EnumerateArray());
        Assert.Equal("8f3a1c", activation.GetProperty("stopId").GetString());
        Assert.Equal(silos[0].GetProperty("name").GetString(), activation.GetProperty("silo").GetString());
    }

    [Fact]
    public async Task GetStats_ReportsTheSourceAndTheActiveGrains()
    {
        await _app.Services.GetRequiredService<IGrainFactory>().GetGrain<IFakeStopGrain>("8f3a1c").PingAsync();

        var stats = await GetJsonAsync("/api/cluster/stats");

        Assert.Equal(FakeSourceGrain.Calls, stats.GetProperty("ingestion").GetProperty("calls").GetInt64());
        Assert.Equal(FakeSourceGrain.CacheHits, stats.GetProperty("ingestion").GetProperty("cacheHits").GetInt64());
        Assert.Equal(1, stats.GetProperty("activeGrains").GetProperty("FakeStopGrain").GetInt32());
    }

    [Fact]
    public async Task KillSilo_WhenChaosIsEnabled_AnswersAcceptedAndKillsTheProcess()
    {
        var response = await _app.Client.PostAsync("/api/cluster/chaos/kill", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, _app.Killer.Kills);
    }

    [Fact]
    public async Task StreamEvents_IsExemptFromTheRequestTimeout()
    {
        var endpoint = _app.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText == "/api/cluster/events");

        Assert.NotNull(endpoint.Metadata.GetMetadata<DisableRequestTimeoutAttribute>());
    }

    private async Task<JsonElement> GetJsonAsync(string path) =>
        await _app.Client.GetFromJsonAsync<JsonElement>(path, TestContext.Current.CancellationToken);
}

public sealed class ClusterChaosDisabledTests
{
    [Fact]
    public async Task KillSilo_WhenChaosIsDisabled_Answers404AndKillsNothing()
    {
        await using var app = await ClusterApp.StartAsync(enableChaos: false);

        var response = await app.Client.PostAsync("/api/cluster/chaos/kill", content: null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("cluster.chaos.disabled", problem.GetProperty("code").GetString());
        Assert.Equal(0, app.Killer.Kills);
    }
}
