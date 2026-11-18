using System.Diagnostics.Metrics;

namespace GleisGrain.Game;

internal sealed class GameMetrics
{
    public const string MeterName = "GleisGrain.Game";

    private readonly Counter<long> _playersClaimed;

    public GameMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _playersClaimed = meter.CreateCounter<long>(
            "gleisgrain.game.players.claimed",
            unit: "{player}",
            description: "Number of nicknames that were claimed.");
    }

    public void PlayerClaimed() => _playersClaimed.Add(1);
}
