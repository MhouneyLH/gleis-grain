using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GleisGrain.Alerts.Tests;

/// <summary>GET /api/alerts through a real web server with the module wired in, on top of the fake source grain.</summary>
[Collection(FakeSourceGrain.Name)]
public class GetAlertsEndpointTests
{
    public GetAlertsEndpointTests() => FakeSourceGrain.Reset();

    [Fact]
    public async Task Get_WithLineAndStopFilter_ReturnsMatchingAlertsAsCamelCaseJson()
    {
        FakeSourceGrain.Notices =
        [
            FakeSourceGrain.Notice("11469_LL_ICSLAG", "L 26: Verlegung Haltestelle Theater", ["26"], ["60500280"]),
            FakeSourceGrain.Notice("12091_LL_ICSLAG", "L 191,192: Umleitung", ["191", "192"]),
        ];
        await using var app = await AlertsTestHost.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var all = await client.GetFromJsonAsync<JsonElement>("/api/alerts", TestContext.Current.CancellationToken);
        var filtered = await client.GetFromJsonAsync<JsonElement>("/api/alerts?line=L26&stop=60500280", TestContext.Current.CancellationToken);
        var emptyFilter = await client.GetFromJsonAsync<JsonElement>("/api/alerts?line=&stop=", TestContext.Current.CancellationToken);

        Assert.Equal(2, all.GetArrayLength());
        Assert.Equal(2, emptyFilter.GetArrayLength());
        var alert = Assert.Single(filtered.EnumerateArray());
        Assert.Equal("11469_LL_ICSLAG", alert.GetProperty("id").GetString());
        Assert.Equal("L 26: Verlegung Haltestelle Theater", alert.GetProperty("title").GetString());
        Assert.Equal("Text of L 26: Verlegung Haltestelle Theater", alert.GetProperty("text").GetString());
        Assert.Equal("26", Assert.Single(alert.GetProperty("lines").EnumerateArray()).GetString());
        Assert.Equal("60500280", Assert.Single(alert.GetProperty("stopIds").EnumerateArray()).GetString());
        Assert.True(alert.TryGetProperty("firstSeenAt", out _));
    }

    [Fact]
    public async Task Get_SourceUnavailableAndNothingKnown_Answers503WithTheErrorCode()
    {
        FakeSourceGrain.Failure = new HttpRequestException("Linz AG is down");
        await using var app = await AlertsTestHost.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var response = await client.GetAsync("/api/alerts", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("alerts.source.unavailable", problem.GetProperty("code").GetString());
    }
}
