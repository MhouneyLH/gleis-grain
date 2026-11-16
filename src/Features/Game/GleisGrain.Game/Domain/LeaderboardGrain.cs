using GleisGrain.Cluster.Contracts;
using GleisGrain.Game.Contracts;

namespace GleisGrain.Game.Domain;

internal sealed class LeaderboardGrain(
    [PersistentState("leaderboard", GameOptions.StorageName)] IPersistentState<LeaderboardState> state,
    ILogger<LeaderboardGrain> logger)
    : Grain, ILeaderboardGrain
{
    private Leaderboard _leaderboard = new();

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _leaderboard = state.State.ToLeaderboard();

        return Task.CompletedTask;
    }

    public async Task ReportScoreAsync(string nickname, int score)
    {
        if (!_leaderboard.Report(nickname, score))
        {
            return;
        }

        state.State = LeaderboardState.From(_leaderboard);
        await state.WriteStateAsync();

        logger.LeaderboardChanged(_leaderboard.Entries.Count);
        var top = _leaderboard.Top(Leaderboard.TopSize).Select(entry => new LeaderboardRow(entry.Nickname, entry.Score));
        await GrainFactory.PublishAsync(logger, new LeaderboardChanged([.. top]));
    }

    public Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int count) =>
        Task.FromResult(_leaderboard.Top(count));
}
