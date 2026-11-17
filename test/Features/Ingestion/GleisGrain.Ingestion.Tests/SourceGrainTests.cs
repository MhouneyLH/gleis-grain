using GleisGrain.Ingestion.Contracts;
using GleisGrain.Ingestion.Feeds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.TestingHost;

namespace GleisGrain.Ingestion.Tests;

/// <summary>
/// SourceGrain in a real silo with a fake feed. One silo serves the whole class (starting several silos
/// one after the other in a process is flaky), so every test uses its own grain key and its own fake feed.
/// </summary>
public class SourceGrainTests(SourceGrainFixture fixture) : IClassFixture<SourceGrainFixture>
{
    private readonly FakeFeed _feed = SourceGrainFixture.UseFakeFeed();
    private readonly ISourceGrain _source = fixture.Cluster.GrainFactory.GetGrain<ISourceGrain>(Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetDepartures_SecondCallWithinTheTimeToLive_IsServedFromTheCache()
    {
        var calledAt = SourceGrainFixture.Time.GetUtcNow();
        var first = await _source.GetDeparturesAsync("60501720");
        var second = await _source.GetDeparturesAsync("60501720");

        Assert.Equal(first, second);
        Assert.Single(_feed.DepartureCalls);
        var stats = await _source.GetStatsAsync();
        Assert.Equal(1, stats.Calls);
        Assert.Equal(1, stats.CacheHits);
        Assert.NotNull(stats.LastCallAt);
        Assert.InRange(stats.LastCallAt.Value, calledAt, SourceGrainFixture.Time.GetUtcNow());
    }

    [Fact]
    public async Task GetDepartures_AfterTheTimeToLive_AsksTheFeedAgain()
    {
        var first = await _source.GetDeparturesAsync("60501720");

        SourceGrainFixture.Time.Advance(TimeSpan.FromSeconds(21));
        var second = await _source.GetDeparturesAsync("60501720");

        Assert.NotEqual(first, second);
        Assert.Equal(2, _feed.DepartureCalls.Count);
    }

    [Fact]
    public async Task GetDepartures_OtherStop_HasItsOwnCacheEntry()
    {
        await _source.GetDeparturesAsync("60501720");

        await RunAsync(_source.GetDeparturesAsync("60500920"));
        await _source.GetDeparturesAsync("60501720");
        await _source.GetDeparturesAsync("60500920");

        Assert.Equal(["60501720", "60500920"], _feed.DepartureCalls);
        Assert.Equal(2, (await _source.GetStatsAsync()).CacheHits);
    }

    [Fact]
    public async Task GetDepartures_CallsOneAfterTheOther_AreSpacedByTheMinimumInterval()
    {
        await _source.GetDeparturesAsync("60501720");

        await RunAsync(_source.GetDeparturesAsync("60500920"));
        await RunAsync(_source.GetNoticesAsync());

        Assert.Equal(3, _feed.CallTimes.Count);
        Assert.True(_feed.CallTimes[1] - _feed.CallTimes[0] >= TimeSpan.FromMilliseconds(490));
        Assert.True(_feed.CallTimes[2] - _feed.CallTimes[1] >= TimeSpan.FromMilliseconds(490));
    }

    [Fact]
    public async Task GetDepartures_FeedFailsWhileAStaleValueExists_ServesTheStaleValue()
    {
        var first = await _source.GetDeparturesAsync("60501720");
        SourceGrainFixture.Time.Advance(TimeSpan.FromSeconds(21));
        _feed.Failure = new HttpRequestException("Linz AG is down");

        var second = await _source.GetDeparturesAsync("60501720");

        Assert.Equal(first, second);
        Assert.Equal(2, (await _source.GetStatsAsync()).Calls);
    }

    [Fact]
    public async Task GetDepartures_FeedFailsWithNothingCached_Throws()
    {
        _feed.Failure = new HttpRequestException("Linz AG is down");

        await Assert.ThrowsAsync<HttpRequestException>(() => _source.GetDeparturesAsync("60501720"));
    }

    [Fact]
    public async Task GetNotices_WithinTheirTimeToLive_AreServedFromTheCache()
    {
        var first = await _source.GetNoticesAsync();

        SourceGrainFixture.Time.Advance(TimeSpan.FromSeconds(59));
        var second = await _source.GetNoticesAsync();

        Assert.Equal(first.Select(notice => notice.Id), second.Select(notice => notice.Id));
        Assert.Equal(1, _feed.NoticeCalls);
    }

    [Fact]
    public async Task GetNotices_AfterTheirTimeToLive_AsksTheFeedAgain()
    {
        await _source.GetNoticesAsync();

        SourceGrainFixture.Time.Advance(TimeSpan.FromSeconds(61));
        await _source.GetNoticesAsync();

        Assert.Equal(2, _feed.NoticeCalls);
    }

    [Fact]
    public async Task GetStats_NothingAskedYet_IsEmpty() =>
        Assert.Equal(new SourceStats(0, 0, null), await _source.GetStatsAsync());

    [Fact]
    public async Task GetNoticesAndDepartures_WithTheReplayFeed_ComeBackThroughTheGrain()
    {
        SourceGrainFixture.UseReplayFeed();

        var departures = await _source.GetDeparturesAsync("60501720");
        var notices = await RunAsync(_source.GetNoticesAsync());

        Assert.NotEmpty(departures);
        Assert.NotEmpty(notices);
        Assert.All(departures, departure => Assert.False(string.IsNullOrEmpty(departure.Line)));
        Assert.All(notices, notice => Assert.False(string.IsNullOrEmpty(notice.Title)));
    }

    /// <summary>Waits for a call that sits out the rate limit in real time, but not forever.</summary>
    private static Task<T> RunAsync<T>(Task<T> call) => call.WaitAsync(TimeSpan.FromSeconds(30));
}

/// <summary>The silo of <see cref="SourceGrainTests"/>. Time and feed are static because Orleans creates the silo configurator itself.</summary>
public sealed class SourceGrainFixture : IAsyncLifetime
{
    private static readonly ShiftableClock _time = new();
    private static readonly SwitchableFeed _feed = new();

    private TestCluster? _cluster;

    public static ShiftableClock Time => _time;

    public TestCluster Cluster => _cluster ?? throw new InvalidOperationException("The cluster is not started.");

    public async ValueTask InitializeAsync()
    {
        var builder = new TestClusterBuilder(1);
        builder.Options.ClusterId = $"ingestion-tests-{Guid.NewGuid():N}";
        builder.Options.ServiceId = $"ingestion-tests-{Guid.NewGuid():N}";
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        _cluster = builder.Build();
        await _cluster.DeployAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.StopAllSilosAsync();
            await _cluster.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    internal static FakeFeed UseFakeFeed()
    {
        var feed = new FakeFeed(_time);
        _feed.Inner = feed;

        return feed;
    }

    internal static void UseReplayFeed() => _feed.Inner = new ReplayFeed(_time, NullLogger<ReplayFeed>.Instance);

    private sealed class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder) =>
            siloBuilder.ConfigureServices(services =>
            {
                services.AddMetrics();
                services.AddSingleton<TimeProvider>(_time);
                services.AddSingleton<IEfaFeed>(_feed);
                services.AddSingleton<IngestionMetrics>();
                services.AddSingleton<IOptions<IngestionOptions>>(Options.Create(new IngestionOptions()));
            });
    }

    private sealed class SwitchableFeed : IEfaFeed
    {
        public IEfaFeed? Inner { get; set; }

        private IEfaFeed Current => Inner ?? throw new InvalidOperationException("No feed chosen for this test.");

        public Task<IReadOnlyList<DepartureSnapshot>> FetchDeparturesAsync(string efaStopId, CancellationToken cancellationToken) =>
            Current.FetchDeparturesAsync(efaStopId, cancellationToken);

        public Task<IReadOnlyList<NoticeSnapshot>> FetchNoticesAsync(CancellationToken cancellationToken) =>
            Current.FetchNoticesAsync(cancellationToken);
    }
}

internal sealed class FakeFeed(TimeProvider time) : IEfaFeed
{
    public List<string> DepartureCalls { get; } = [];

    public int NoticeCalls { get; private set; }

    /// <summary>When every call happened, in order, to check the spacing.</summary>
    public List<DateTimeOffset> CallTimes { get; } = [];

    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<DepartureSnapshot>> FetchDeparturesAsync(string efaStopId, CancellationToken cancellationToken)
    {
        DepartureCalls.Add(efaStopId);
        CallTimes.Add(time.GetUtcNow());

        // Every call answers differently, so a test can tell a fresh answer from a cached one.
        return Failure is null
            ? Task.FromResult<IReadOnlyList<DepartureSnapshot>>(new List<DepartureSnapshot> { new("1", $"Call {CallTimes.Count}", time.GetUtcNow(), 0, null) })
            : Task.FromException<IReadOnlyList<DepartureSnapshot>>(Failure);
    }

    public Task<IReadOnlyList<NoticeSnapshot>> FetchNoticesAsync(CancellationToken cancellationToken)
    {
        NoticeCalls++;
        CallTimes.Add(time.GetUtcNow());

        return Failure is null
            ? Task.FromResult<IReadOnlyList<NoticeSnapshot>>(new List<NoticeSnapshot> { new($"n{CallTimes.Count}", "L 1: Umleitung", "Text", new[] { "1" }, Array.Empty<string>()) })
            : Task.FromException<IReadOnlyList<NoticeSnapshot>>(Failure);
    }
}
