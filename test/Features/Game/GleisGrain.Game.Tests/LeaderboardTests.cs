using GleisGrain.Game.Contracts;
using GleisGrain.Game.Domain;

namespace GleisGrain.Game.Tests;

public class LeaderboardTests
{
    [Fact]
    public void Top_OrdersByScoreDescendingThenNickname()
    {
        var board = new Leaderboard();
        board.Report("Charlie", 3);
        board.Report("Bea", 5);
        board.Report("Alex", 3);

        var top = board.Top(10);

        Assert.Equal(
            [new LeaderboardEntry("Bea", 5), new LeaderboardEntry("Alex", 3), new LeaderboardEntry("Charlie", 3)],
            top);
    }

    [Fact]
    public void Top_ReturnsAtMostTheRequestedCount()
    {
        var board = new Leaderboard();
        board.Report("Alex", 1);
        board.Report("Bea", 2);
        board.Report("Max", 3);

        var top = board.Top(2);

        Assert.Equal(["Max", "Bea"], top.Select(entry => entry.Nickname));
    }

    [Fact]
    public void Report_SameNicknameInOtherCase_UpdatesTheSamePlayer()
    {
        var board = new Leaderboard();
        board.Report("Max", 1);

        var changed = board.Report("MAX", 4);

        Assert.True(changed);
        Assert.Single(board.Top(10));
        Assert.Equal(4, board.Top(10)[0].Score);
    }

    [Fact]
    public void Report_WithTheScoreItAlreadyHas_ReportsNoChange()
    {
        var board = new Leaderboard();
        board.Report("Max", 3);

        Assert.False(board.Report("max", 3));
    }

    [Fact]
    public void Report_WithALowerScore_IsIgnored()
    {
        var board = new Leaderboard();
        board.Report("Max", 6);

        var changed = board.Report("Max", 3);

        Assert.False(changed);
        Assert.Equal(6, board.Top(10)[0].Score);
    }

    [Fact]
    public void Report_ForANewPlayer_ReportsAChange() =>
        Assert.True(new Leaderboard().Report("Max", 0));

    [Fact]
    public void Constructor_WithEntries_RestoresTheRanking()
    {
        var board = new Leaderboard([new LeaderboardEntry("Alex", 1), new LeaderboardEntry("Bea", 2)]);

        Assert.Equal(["Bea", "Alex"], board.Top(10).Select(entry => entry.Nickname));
    }
}
