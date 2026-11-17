using GleisGrain.Alerts.Domain;
using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Alerts.Tests;

public class AlertBoardTests
{
    private static readonly DateTimeOffset _start = new(2026, 11, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Refresh_NewNotices_AreAddedWithTheTimeTheyWereSeen()
    {
        var board = new AlertBoard();

        var changed = board.Refresh([Notice("a", "L 26: Verlegung", lines: ["26"]), Notice("b", "Umleitung")], _start);

        Assert.True(changed);
        Assert.Equal(["a", "b"], board.Alerts.Select(alert => alert.Id.Value));
        Assert.All(board.Alerts, alert => Assert.Equal(_start, alert.FirstSeenAt));
        Assert.Equal(_start, board.RefreshedAt);
    }

    [Fact]
    public void Refresh_KnownNotice_KeepsFirstSeenAtAndTakesTheNewContent()
    {
        var board = new AlertBoard();
        board.Refresh([Notice("a", "Alt")], _start);

        var changed = board.Refresh([Notice("a", "Neu")], _start.AddMinutes(5));

        var alert = Assert.Single(board.Alerts);
        Assert.True(changed);
        Assert.Equal("Neu", alert.Title);
        Assert.Equal(_start, alert.FirstSeenAt);
    }

    [Fact]
    public void Refresh_NoticeThatDisappeared_IsDropped()
    {
        var board = new AlertBoard();
        board.Refresh([Notice("a", "A"), Notice("b", "B")], _start);

        var changed = board.Refresh([Notice("b", "B")], _start.AddMinutes(5));

        Assert.True(changed);
        Assert.Equal(["b"], board.Alerts.Select(alert => alert.Id.Value));
    }

    [Fact]
    public void Refresh_SameNotices_ReportsNoChangeButCountsAsRefreshed()
    {
        var board = new AlertBoard();
        board.Refresh([Notice("a", "A", lines: ["1"])], _start);

        var changed = board.Refresh([Notice("a", "A", lines: ["1"])], _start.AddMinutes(5));

        Assert.False(changed);
        Assert.Equal(_start.AddMinutes(5), board.RefreshedAt);
    }

    [Fact]
    public void Refresh_NoNoticesOnAnEmptyBoard_ReportsNoChange() =>
        Assert.False(new AlertBoard().Refresh([], _start));

    [Fact]
    public void Refresh_TwoNoticesWithTheSameId_ShowOnlyTheFirst()
    {
        var board = new AlertBoard();

        board.Refresh([Notice("a", "Erste"), Notice("a", "Zweite")], _start);

        Assert.Equal("Erste", Assert.Single(board.Alerts).Title);
    }

    [Fact]
    public void Refresh_NoticeIdThatIsNotAValidAlertId_GetsADerivedId()
    {
        var board = new AlertBoard();

        board.Refresh([Notice("Ünsafe id/with spaces", "A")], _start);

        Assert.StartsWith("h-", Assert.Single(board.Alerts).Id.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void NeedsRefresh_NeverRefreshed_IsTrue() =>
        Assert.True(new AlertBoard().NeedsRefresh(_start, TimeSpan.FromMinutes(2)));

    [Theory]
    [InlineData(119, false)]
    [InlineData(120, true)]
    public void NeedsRefresh_AfterARefresh_IsTrueOnceItIsStale(int seconds, bool expected)
    {
        var board = new AlertBoard();
        board.Refresh([], _start);

        Assert.Equal(expected, board.NeedsRefresh(_start.AddSeconds(seconds), TimeSpan.FromMinutes(2)));
    }

    [Theory]
    [InlineData("26")]
    [InlineData("L 26")]
    [InlineData("l26")]
    [InlineData("Linie 26")]
    [InlineData(" 26 ")]
    public void Filter_LineSpelledInATolerantWay_MatchesANoticeNamingL26(string line)
    {
        var board = BoardWith(Notice("a", "L 26: Verlegung", lines: ["L 26"]), Notice("b", "L 2: Umleitung", lines: ["2"]));

        Assert.Equal(["a"], board.Filter(line, null).Select(alert => alert.Id.Value));
    }

    [Fact]
    public void Filter_LineNamedAmongSeveral_Matches()
    {
        var board = BoardWith(Notice("a", "L 191,192: Umleitung", lines: ["191", "192"]));

        Assert.Single(board.Filter("192", null));
        Assert.Empty(board.Filter("19", null));
    }

    [Fact]
    public void Filter_Stop_MatchesNoticesNamingIt()
    {
        var board = BoardWith(Notice("a", "A", stopIds: ["60501720"]), Notice("b", "B", stopIds: ["60500920"]), Notice("c", "C"));

        Assert.Equal(["a"], board.Filter(null, "60501720").Select(alert => alert.Id.Value));
    }

    [Fact]
    public void Filter_LineAndStop_NeedsBoth()
    {
        var board = BoardWith(
            Notice("a", "A", lines: ["1"], stopIds: ["60501720"]),
            Notice("b", "B", lines: ["1"], stopIds: ["60500920"]),
            Notice("c", "C", lines: ["2"], stopIds: ["60501720"]));

        Assert.Equal(["a"], board.Filter("1", "60501720").Select(alert => alert.Id.Value));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("  ", null)]
    public void Filter_NothingGiven_ReturnsEveryNotice(string? line, string? stopId)
    {
        var board = BoardWith(Notice("a", "A", lines: ["1"]), Notice("b", "B"));

        Assert.Equal(2, board.Filter(line, stopId).Count);
    }

    [Fact]
    public void Filter_NoticeNamingNoLine_DoesNotMatchALineFilter()
    {
        var board = BoardWith(Notice("a", "Umleitung"));

        Assert.Empty(board.Filter("1", null));
    }

    [Fact]
    public void State_RoundTrip_KeepsAlertsAndForgetsTheRefreshTime()
    {
        var board = BoardWith(Notice("a", "L 26: Verlegung", lines: ["26"], stopIds: ["60501720"]));

        var restored = AlertBoardState.From(board).ToBoard();

        var alert = Assert.Single(restored.Alerts);
        Assert.Equal("a", alert.Id.Value);
        Assert.Equal("L 26: Verlegung", alert.Title);
        Assert.Equal(["26"], alert.Lines);
        Assert.Equal(["60501720"], alert.StopIds);
        Assert.Equal(_start, alert.FirstSeenAt);
        Assert.Null(restored.RefreshedAt);
    }

    private static AlertBoard BoardWith(params NoticeSnapshot[] notices)
    {
        var board = new AlertBoard();
        board.Refresh(notices, _start);

        return board;
    }

    private static NoticeSnapshot Notice(string id, string title, string[]? lines = null, string[]? stopIds = null) =>
        new(id, title, "Text of " + title, lines ?? [], stopIds ?? []);
}
