using System.Text.Json;
using GleisGrain.Cluster.Contracts;
using GleisGrain.Game.Contracts;

namespace GleisGrain.Game.Tests;

public class LeaderboardGrainTests(GameCluster game) : IClassFixture<GameCluster>
{
    private ILeaderboardGrain Board => game.Grains.GetGrain<ILeaderboardGrain>(LeaderboardKeys.Main);

    [Fact]
    public async Task GetTopAsync_AfterReports_OrdersByScoreThenNickname()
    {
        var tag = "lb" + GameCluster.NewId()[..6];
        await Board.ReportScoreAsync($"{tag}-c", 3);
        await Board.ReportScoreAsync($"{tag}-b", 5);
        await Board.ReportScoreAsync($"{tag}-a", 3);

        var mine = await Eventually(tag, expected: 3);

        Assert.Equal([$"{tag}-b", $"{tag}-a", $"{tag}-c"], mine.Select(entry => entry.Nickname));
    }

    [Fact]
    public async Task ReportScoreAsync_SameNicknameInOtherCase_UpdatesTheSameEntry()
    {
        var tag = "lc" + GameCluster.NewId()[..6];
        await Board.ReportScoreAsync($"{tag}-x", 1);
        await Board.ReportScoreAsync($"{tag}-X", 4);

        var mine = await Eventually(tag, expected: 1, score: 4);

        Assert.Equal(4, Assert.Single(mine).Score);
    }

    [Fact]
    public async Task ReportScoreAsync_ChangedRanking_PublishesLeaderboardChanged()
    {
        var nickname = "ld" + GameCluster.NewId()[..6];

        await Board.ReportScoreAsync(nickname, 7);

        var events = await GameCluster.EventsAsync(game.Grains, LeaderboardChanged.EventType, nickname);
        var payload = JsonSerializer.Deserialize<LeaderboardChanged>(events[0].Json, JsonSerializerOptions.Web);
        Assert.Contains(new LeaderboardRow(nickname, 7), payload!.Top);
        Assert.True(payload.Top.Count <= 10);
    }

    private async Task<IReadOnlyList<LeaderboardEntry>> Eventually(string tag, int expected, int? score = null) =>
        await GameCluster.EventuallyAsync(
            async () => (IReadOnlyList<LeaderboardEntry>)[.. (await Board.GetTopAsync(1000)).Where(entry => entry.Nickname.StartsWith(tag, StringComparison.Ordinal))],
            entries => entries.Count >= expected && (score is null || entries.All(entry => entry.Score == score)));
}
