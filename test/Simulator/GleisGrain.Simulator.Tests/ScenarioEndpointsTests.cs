using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GleisGrain.Simulator.Tests;

public sealed class ScenarioEndpointsTests : IAsyncLifetime
{
    private const string Catalogue = """[{"id":"60501720","name":"Hauptbahnhof"},{"id":"stop-1","name":"One"}]""";

    private readonly DownstreamHandler _downstream = new();
    private WebApplication? _app;
    private HttpClient? _client;

    private HttpClient Client => _client!;

    public async ValueTask InitializeAsync()
    {
        _app = SimulatorApp.Build(Args(), services => Downstream(services, _downstream));
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };

        // The catalogue loads on the first tick of the background worker.
        await WaitForAsync(async () => (await GetScenarioAsync()).GetProperty("people").GetInt32() > 0);
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        _downstream.Dispose();
    }

    [Fact]
    public async Task GetScenario_AfterStart_ReturnsTheCamelCaseShape()
    {
        var response = await Client.GetAsync(new Uri("/scenario", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["people", "joined", "pinned", "hotStopShare"], body.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.Equal(4, body.GetProperty("people").GetInt32());
        Assert.Equal(0, body.GetProperty("pinned").GetArrayLength());
        Assert.Equal(0.4, body.GetProperty("hotStopShare").GetDouble());
    }

    [Fact]
    public async Task PutPinned_ValidRequest_ReturnsTheNewScenario()
    {
        var response = await PutAsync("/scenario/pinned/60501720", """{"people":6}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(6, body.GetProperty("people").GetInt32());
        var pinned = Assert.Single(body.GetProperty("pinned").EnumerateArray());
        Assert.Equal("60501720", pinned.GetProperty("stopId").GetString());
        Assert.Equal(6, pinned.GetProperty("people").GetInt32());
        Assert.Equal(6, (await GetScenarioAsync()).GetProperty("pinned")[0].GetProperty("people").GetInt32());
    }

    [Fact]
    public async Task PutPinned_UnknownStop_Returns400ProblemDetails()
    {
        var response = await PutAsync("/scenario/pinned/nowhere", """{"people":3}""");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("""{"people":-1}""")]
    [InlineData("""{"people":501}""")]
    [InlineData("""{}""")]
    [InlineData("""{"people":"many"}""")]
    public async Task PutPinned_InvalidPeople_Returns400ProblemDetails(string json)
    {
        var response = await PutAsync("/scenario/pinned/60501720", json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutPinned_TotalAboveMaxPeople_Returns400ProblemDetails()
    {
        (await PutAsync("/scenario/pinned/60501720", """{"people":300}""")).EnsureSuccessStatusCode();

        var response = await PutAsync("/scenario/pinned/stop-1", """{"people":201}""");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutPinned_Zero_RemovesThePin()
    {
        (await PutAsync("/scenario/pinned/60501720", """{"people":3}""")).EnsureSuccessStatusCode();

        var response = await PutAsync("/scenario/pinned/60501720", """{"people":0}""");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(0, body.GetProperty("pinned").GetArrayLength());
    }

    [Fact]
    public async Task DeletePinned_AfterPinning_ReleasesEveryone()
    {
        (await PutAsync("/scenario/pinned/60501720", """{"people":3}""")).EnsureSuccessStatusCode();
        (await PutAsync("/scenario/pinned/stop-1", """{"people":2}""")).EnsureSuccessStatusCode();

        var response = await Client.DeleteAsync(new Uri("/scenario/pinned", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(0, body.GetProperty("pinned").GetArrayLength());
    }

    [Fact]
    public async Task PutPinned_CatalogueNotLoaded_Returns503ProblemDetails()
    {
        using var unreachable = new DownstreamHandler { Unreachable = true };
        await using var app = SimulatorApp.Build(Args(), services => Downstream(services, unreachable));
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        using var content = new StringContent("""{"people":3}""", Encoding.UTF8, "application/json");
        var response = await client.PutAsync(new Uri("/scenario/pinned/60501720", UriKind.Relative), content, TestContext.Current.CancellationToken);

        await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    private static string[] Args() =>
        ["--Simulator:Port=0", "--Simulator:People=4", "--Simulator:RampUpSeconds=0", "--Simulator:MaxPeople=500", "--Simulator:Seed=7", "--Logging:LogLevel:Default=Warning"];

    private static void Downstream(IServiceCollection services, DownstreamHandler handler) =>
        services.AddHttpClient<GleisGrainClient>().ConfigurePrimaryHttpMessageHandler(() => handler);

    private async Task<JsonElement> GetScenarioAsync() =>
        await Client.GetFromJsonAsync<JsonElement>(new Uri("/scenario", UriKind.Relative), TestContext.Current.CancellationToken);

    private async Task<HttpResponseMessage> PutAsync(string path, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await Client.PutAsync(new Uri(path, UriKind.Relative), content, TestContext.Current.CancellationToken);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!await condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The simulator did not load the catalogue in time.");
            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    private sealed class DownstreamHandler : HttpMessageHandler
    {
        public bool Unreachable { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Unreachable)
            {
                throw new HttpRequestException("connection refused");
            }

            var response = request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/stops"
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Catalogue, Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(request.Method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.NoContent);
            return Task.FromResult(response);
        }
    }
}
