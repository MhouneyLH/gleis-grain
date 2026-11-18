using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace GleisGrain.Simulator.Tests;

public sealed class AudienceSimulatorTests : IDisposable
{
    private const string Catalogue = """[{"id":"60501720","name":"Hauptbahnhof"},{"id":"stop-1","name":"One"},{"id":"stop-2","name":"Two"}]""";

    private readonly FakeTimeProvider _time = new(DateTimeOffset.UnixEpoch);
    private readonly FakeHandler _handler = new();
    private readonly HttpClient _http;
    private readonly AudienceSimulator _simulator;

    public AudienceSimulatorTests()
    {
        _http = new HttpClient(_handler) { BaseAddress = new Uri("http://gateway.test") };
        var options = Options.Create(new SimulatorOptions { People = 3, RampUpSeconds = 0, Seed = 7 });
        _simulator = new AudienceSimulator(new GleisGrainClient(_http), options, _time, new Random(7), NullLogger<AudienceSimulator>.Instance);
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
    }

    [Fact]
    public async Task TickAsync_FirstTick_LoadsTheCatalogueOnly()
    {
        await _simulator.TickAsync(CancellationToken.None);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(("GET", "/api/stops"), (request.Method, request.Path));
    }

    [Fact]
    public async Task TickAsync_SecondTick_ClaimsEachNicknameAndMovesToAStop()
    {
        await _simulator.TickAsync(CancellationToken.None);
        _handler.Requests.Clear();

        await _simulator.TickAsync(CancellationToken.None);

        var claims = _handler.Requests.Where(r => r.Method == "POST").ToList();
        Assert.Equal(3, claims.Count);
        Assert.All(claims, claim => Assert.Equal("/api/game/players", claim.Path));
        Assert.Equal(
            ["""{"nickname":"Sim-001"}""", """{"nickname":"Sim-002"}""", """{"nickname":"Sim-003"}"""],
            claims.Select(claim => claim.Body).Order().ToArray());

        var moves = _handler.Requests.Where(r => r.Method == "PUT").ToList();
        Assert.Equal(3, moves.Count);
        Assert.All(moves, move => Assert.Matches("^/api/game/players/Sim-00[123]/stop$", move.Path));
        Assert.All(moves, move => Assert.Matches("""^\{"stopId":"(60501720|stop-1|stop-2)"\}$""", move.Body));
    }

    [Fact]
    public async Task TickAsync_NicknameTaken_CountsAsJoined()
    {
        _handler.Respond = request => request.Method == HttpMethod.Post ? HttpStatusCode.Conflict : HttpStatusCode.NoContent;
        await _simulator.TickAsync(CancellationToken.None);
        await _simulator.TickAsync(CancellationToken.None);
        _handler.Requests.Clear();

        _time.Advance(TimeSpan.FromSeconds(18));
        await _simulator.TickAsync(CancellationToken.None);

        Assert.All(_handler.Requests, request => Assert.Equal("GET", request.Method));
        Assert.Matches("^/api/stops/(60501720|stop-1|stop-2)$", _handler.Requests[0].Path);
    }

    [Fact]
    public async Task TickAsync_ServerError_DoesNotThrowAndRetriesLater()
    {
        await _simulator.TickAsync(CancellationToken.None);
        _handler.Respond = _ => HttpStatusCode.InternalServerError;

        await _simulator.TickAsync(CancellationToken.None);
        var failedCalls = _handler.Requests.Count;
        await _simulator.TickAsync(CancellationToken.None);

        Assert.Equal(failedCalls, _handler.Requests.Count);

        _handler.Respond = _ => HttpStatusCode.NoContent;
        _time.Advance(TimeSpan.FromSeconds(6));
        _handler.Requests.Clear();
        await _simulator.TickAsync(CancellationToken.None);

        Assert.Contains(_handler.Requests, request => request.Method == "PUT");
    }

    [Fact]
    public async Task TickAsync_HostUnreachable_DoesNotThrowAndRetriesTheCatalogue()
    {
        _handler.Fail = new HttpRequestException("connection refused");

        await _simulator.TickAsync(CancellationToken.None);
        await _simulator.TickAsync(CancellationToken.None);
        Assert.Single(_handler.Requests);

        _handler.Fail = null;
        _time.Advance(TimeSpan.FromSeconds(4));
        await _simulator.TickAsync(CancellationToken.None);

        Assert.Equal(2, _handler.Requests.Count);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        public Exception? Fail { get; set; }

        public Func<HttpRequestMessage, HttpStatusCode> Respond { get; set; } =
            request => request.Method == HttpMethod.Post ? HttpStatusCode.Created : HttpStatusCode.NoContent;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            lock (Requests)
            {
                Requests.Add(new RecordedRequest(request.Method.Method, request.RequestUri!.AbsolutePath, body));
            }

            if (Fail is not null)
            {
                throw Fail;
            }

            if (request.Method == HttpMethod.Get)
            {
                var json = request.RequestUri.AbsolutePath == "/api/stops" ? Catalogue : "{}";
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }

            return new HttpResponseMessage(Respond(request));
        }
    }

    private sealed record RecordedRequest(string Method, string Path, string Body);
}
