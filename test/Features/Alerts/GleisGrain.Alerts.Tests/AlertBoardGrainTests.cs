using GleisGrain.Alerts.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace GleisGrain.Alerts.Tests;

/// <summary>
/// AlertBoardGrain in a real silo with in-memory storage and reminders and a fake source grain (a real web host, see AlertsTestHost).
/// One silo serves the whole class, so every test uses its own grain key.
/// </summary>
[Collection(FakeSourceGrain.Name)]
public class AlertBoardGrainTests : IClassFixture<AlertBoardGrainFixture>
{
    private readonly AlertBoardGrainFixture _fixture;
    private readonly string _key = Guid.NewGuid().ToString("N");

    public AlertBoardGrainTests(AlertBoardGrainFixture fixture)
    {
        _fixture = fixture;
        FakeSourceGrain.Reset();
    }

    private IAlertBoardGrain Board => _fixture.Grains.GetGrain<IAlertBoardGrain>(_key);

    [Fact]
    public async Task GetAsync_FirstUse_LoadsTheNoticesFromTheSource()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "L 26: Verlegung", ["26"]), FakeSourceGrain.Notice("b", "Umleitung")];

        var before = _fixture.Time.GetUtcNow();
        var alerts = await Board.GetAsync(null, null);

        Assert.Equal(["a", "b"], alerts.Select(alert => alert.Id.Value));
        Assert.Equal("L 26: Verlegung", alerts[0].Title);
        Assert.Equal(["26"], alerts[0].Lines);
        Assert.InRange(alerts[0].FirstSeenAt, before, _fixture.Time.GetUtcNow());
    }

    [Fact]
    public async Task GetAsync_BoardIsFresh_DoesNotAskTheSourceAgain()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A")];
        await Board.GetAsync(null, null);

        _fixture.Time.Advance(TimeSpan.FromSeconds(119));
        await Board.GetAsync(null, null);

        Assert.Equal(1, FakeSourceGrain.NoticeCalls);
    }

    [Fact]
    public async Task GetAsync_BoardIsStale_RefreshesFirstAndKeepsWhenAlertsWereFirstSeen()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A")];
        var first = await Board.GetAsync(null, null);
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A"), FakeSourceGrain.Notice("b", "B")];

        _fixture.Time.Advance(TimeSpan.FromMinutes(3));
        var second = await Board.GetAsync(null, null);

        Assert.Equal(2, FakeSourceGrain.NoticeCalls);
        Assert.Equal(["a", "b"], second.Select(alert => alert.Id.Value));
        Assert.Equal(first[0].FirstSeenAt, second[0].FirstSeenAt);
        Assert.InRange(second[1].FirstSeenAt - first[0].FirstSeenAt, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3.5));
    }

    [Fact]
    public async Task GetAsync_NoticeGoneFromTheSource_IsDroppedFromTheBoard()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A"), FakeSourceGrain.Notice("b", "B")];
        await Board.GetAsync(null, null);
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("b", "B")];

        _fixture.Time.Advance(TimeSpan.FromMinutes(3));
        var alerts = await Board.GetAsync(null, null);

        Assert.Equal(["b"], alerts.Select(alert => alert.Id.Value));
    }

    [Fact]
    public async Task GetAsync_LineAndStopFilter_ReturnsOnlyMatchingAlerts()
    {
        FakeSourceGrain.Notices =
        [
            FakeSourceGrain.Notice("a", "L 26: Verlegung", ["26"], ["60500280"]),
            FakeSourceGrain.Notice("b", "L 2: Umleitung", ["2"]),
        ];

        Assert.Equal(["a"], (await Board.GetAsync("L 26", null)).Select(alert => alert.Id.Value));
        Assert.Equal(["a"], (await Board.GetAsync(null, "60500280")).Select(alert => alert.Id.Value));
        Assert.Empty(await Board.GetAsync("26", "60501720"));
        Assert.Equal(2, (await Board.GetAsync(null, null)).Count);
    }

    [Fact]
    public async Task GetAsync_SourceFailsAfterABoardExists_ServesTheKnownAlerts()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A")];
        await Board.GetAsync(null, null);
        FakeSourceGrain.Failure = new HttpRequestException("Linz AG is down");

        _fixture.Time.Advance(TimeSpan.FromMinutes(3));
        var alerts = await Board.GetAsync(null, null);

        Assert.Equal(["a"], alerts.Select(alert => alert.Id.Value));
        Assert.Equal(2, FakeSourceGrain.NoticeCalls);
    }

    [Fact]
    public async Task GetAsync_SourceFailsOnFirstUse_Throws()
    {
        FakeSourceGrain.Failure = new HttpRequestException("Linz AG is down");

        await Assert.ThrowsAsync<HttpRequestException>(() => Board.GetAsync(null, null));
    }

    [Fact]
    public async Task GetAsync_AfterTheGrainWasDeactivated_StillKnowsTheAlertsAndWhenTheyWereSeen()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A")];
        var first = await Board.GetAsync(null, null);
        FakeSourceGrain.Failure = new HttpRequestException("Linz AG is down");

        await _fixture.Grains.GetGrain<IManagementGrain>(0).ForceActivationCollection(TimeSpan.Zero, TestContext.Current.CancellationToken);
        _fixture.Time.Advance(TimeSpan.FromMinutes(10));
        var second = await Board.GetAsync(null, null);

        Assert.Equal(first[0].FirstSeenAt, Assert.Single(second).FirstSeenAt);
    }

    [Fact]
    public async Task ReceiveReminder_RefreshesTheBoardWithoutARequest()
    {
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A")];
        await Board.GetAsync(null, null);
        FakeSourceGrain.Notices = [FakeSourceGrain.Notice("a", "A"), FakeSourceGrain.Notice("b", "B")];

        await Board.AsReference<IRemindable>().ReceiveReminder("refresh", new TickStatus(DateTime.UtcNow, TimeSpan.FromMinutes(1), DateTime.UtcNow), TestContext.Current.CancellationToken);

        Assert.Equal(2, FakeSourceGrain.NoticeCalls);
        _fixture.Time.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(["a", "b"], (await Board.GetAsync(null, null)).Select(alert => alert.Id.Value));
        Assert.Equal(2, FakeSourceGrain.NoticeCalls);
    }
}

/// <summary>The silo of <see cref="AlertBoardGrainTests"/>: a real web host with the module wired in, on a shiftable clock.</summary>
public sealed class AlertBoardGrainFixture : IAsyncLifetime
{
    private WebApplication? _app;

    public ShiftableClock Time { get; } = new();

    public IGrainFactory Grains => _app?.Services.GetRequiredService<IGrainFactory>() ?? throw new InvalidOperationException("The silo is not started.");

    public async ValueTask InitializeAsync() =>
        _app = await AlertsTestHost.StartAsync(Time, reminderPeriod: TimeSpan.FromDays(1));

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
