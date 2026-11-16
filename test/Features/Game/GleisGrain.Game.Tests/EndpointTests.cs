using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using GleisGrain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GleisGrain.Game.Tests;

/// <summary>The game endpoints on a real web server with a local silo.</summary>
public class EndpointTests
{
    [Fact]
    public async Task ClaimPlayer_NicknameThatIsTaken_Answers409WithTheErrorCode()
    {
        await using var app = await StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        var nickname = "Max" + Guid.NewGuid().ToString("N")[..6];

        var first = await client.PostAsJsonAsync("/api/game/players", new { nickname }, TestContext.Current.CancellationToken);
        var second = await client.PostAsJsonAsync("/api/game/players", new { nickname = nickname.ToUpperInvariant() }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        using var problem = JsonDocument.Parse(await second.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("game.nickname.taken", problem.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("x")]
    [InlineData("no/slash")]
    [InlineData("a-nickname-way-too-long-to-be-valid")]
    public async Task ClaimPlayer_InvalidNickname_Answers400(string nickname)
    {
        await using var app = await StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var response = await client.PostAsJsonAsync("/api/game/players", new { nickname }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceBet_ThenGetPlayer_ShowTheOpenBetInTheDocumentedShape()
    {
        await using var app = await StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        var grains = app.Services.GetRequiredService<IGrainFactory>();
        var nickname = "Max" + Guid.NewGuid().ToString("N")[..6];
        var tripId = "60501720:1:" + Guid.NewGuid().ToString("N")[..8];
        await GameCluster.SetTripAsync(grains, tripId);
        await client.PostAsJsonAsync("/api/game/players", new { nickname }, TestContext.Current.CancellationToken);

        var placed = await client.PostAsJsonAsync($"/api/game/players/{nickname}/bet", new { tripId, guessedDelayMinutes = 2 }, TestContext.Current.CancellationToken);
        var fetched = await client.GetAsync($"/api/game/players/{nickname.ToLowerInvariant()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        using var player = JsonDocument.Parse(await fetched.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(nickname, player.RootElement.GetProperty("nickname").GetString());
        Assert.Equal(0, player.RootElement.GetProperty("score").GetInt32());
        Assert.Equal(JsonValueKind.Null, player.RootElement.GetProperty("watchedStopId").ValueKind);
        var bet = player.RootElement.GetProperty("openBet");
        Assert.Equal(tripId, bet.GetProperty("tripId").GetString());
        Assert.Equal("60501720", bet.GetProperty("stopId").GetString());
        Assert.Equal(2, bet.GetProperty("guessedDelayMinutes").GetInt32());
    }

    [Fact]
    public async Task PlaceBet_GuessOutsideZeroToFifteen_Answers400WithTheErrorCode()
    {
        await using var app = await StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        var nickname = "Max" + Guid.NewGuid().ToString("N")[..6];
        await client.PostAsJsonAsync("/api/game/players", new { nickname }, TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync($"/api/game/players/{nickname}/bet", new { tripId = "any", guessedDelayMinutes = 16 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("game.bet.invalid_guess", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WatchStop_ForAnUnknownPlayer_Answers404WithTheErrorCode()
    {
        await using var app = await StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var response = await client.PutAsJsonAsync("/api/game/players/Ghost/stop", new { stopId = "60501720" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("game.player.not_found", problem.RootElement.GetProperty("code").GetString());
    }

    private static async Task<WebApplication> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{FreePort()}");
        builder.AddGleisGrainHttp();
        builder.AddGameModule();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: FreePort());
            silo.AddMemoryGrainStorage(GameModule.StorageName);
            silo.UseInMemoryReminderService();
        });

        var app = builder.Build();
        app.UseGleisGrainHttp();
        app.MapGroup("/api").MapGameEndpoints();
        await app.StartAsync();

        return app;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
