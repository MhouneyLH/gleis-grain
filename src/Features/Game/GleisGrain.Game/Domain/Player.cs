using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>
/// Pure domain model: no Orleans runtime, no I/O. A person with a score, a watched stop and at most one open bet.
/// The grain loads it from its state, calls it and persists the result.
/// </summary>
internal sealed class Player(Nickname nickname, int score = 0, StopId? watchedStop = null, Bet? openBet = null)
{
    public Nickname Nickname { get; } = nickname;

    public int Score { get; private set; } = score;

    public StopId? WatchedStop { get; private set; } = watchedStop;

    public Bet? OpenBet { get; private set; } = openBet;

    public void Watch(StopId stopId) => WatchedStop = stopId;

    /// <summary>A player has one open bet at a time. False when there already is one.</summary>
    public bool TryPlaceBet(Bet bet)
    {
        if (OpenBet is not null)
        {
            return false;
        }

        OpenBet = bet;

        return true;
    }

    /// <summary>Scores the open bet against the final delay and clears it. Null when there is no open bet.</summary>
    public BetResolution? Resolve(int actualDelayMinutes)
    {
        if (OpenBet is not { } bet)
        {
            return null;
        }

        var points = Scoring.Points(bet.GuessedDelayMinutes, actualDelayMinutes);
        Score += points;
        OpenBet = null;

        return new BetResolution(bet.TripId, bet.GuessedDelayMinutes, actualDelayMinutes, points);
    }

    public PlayerSnapshot ToSnapshot() =>
        new(Nickname, Score, WatchedStop, OpenBet is { } bet
            ? new Contracts.OpenBet(bet.TripId, bet.StopId, bet.Line, bet.PlannedAt, bet.GuessedDelayMinutes)
            : null);
}
