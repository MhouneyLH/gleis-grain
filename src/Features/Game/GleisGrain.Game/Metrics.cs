using System.Diagnostics.Metrics;

namespace GleisGrain.Game;

internal sealed class GameMetrics
{
    public const string MeterName = "GleisGrain.Game";

    private readonly Counter<long> _playersClaimed;
    private readonly Counter<long> _betsPlaced;
    private readonly Counter<long> _betsResolved;

    public GameMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _playersClaimed = meter.CreateCounter<long>(
            "gleisgrain.game.players.claimed",
            unit: "{player}",
            description: "Number of nicknames that were claimed.");
        _betsPlaced = meter.CreateCounter<long>(
            "gleisgrain.game.bets.placed",
            unit: "{bet}",
            description: "Number of bets that were placed.");
        _betsResolved = meter.CreateCounter<long>(
            "gleisgrain.game.bets.resolved",
            unit: "{bet}",
            description: "Number of bets that were resolved, tagged by the points they earned.");
    }

    public void PlayerClaimed() => _playersClaimed.Add(1);

    public void BetPlaced() => _betsPlaced.Add(1);

    public void BetResolved(int points) =>
        _betsResolved.Add(1, new KeyValuePair<string, object?>("points", points));
}
