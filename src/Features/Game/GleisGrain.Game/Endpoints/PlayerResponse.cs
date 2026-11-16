using System.ComponentModel;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Endpoints;

/// <summary>The player as every game endpoint returns it.</summary>
internal sealed record PlayerResponse
{
    [Description("The nickname as it was claimed.")]
    public required Nickname Nickname { get; init; }

    [Description("Points earned so far.")]
    public required int Score { get; init; }

    [Description("The stop the player watches, null when none.")]
    public required StopId? WatchedStopId { get; init; }

    [Description("The bet that is waiting for its departure, null when none.")]
    public required OpenBetResponse? OpenBet { get; init; }

    public static PlayerResponse From(PlayerSnapshot player) => new()
    {
        Nickname = player.Nickname,
        Score = player.Score,
        WatchedStopId = player.WatchedStop,
        OpenBet = player.OpenBet is { } bet
            ? new OpenBetResponse
            {
                TripId = bet.TripId,
                StopId = bet.StopId,
                Line = bet.Line,
                PlannedAt = bet.PlannedAt,
                GuessedDelayMinutes = bet.GuessedDelayMinutes,
            }
            : null,
    };
}

internal sealed record OpenBetResponse
{
    [Description("Identifier of the trip the bet is on.")]
    public required TripId TripId { get; init; }

    [Description("Identifier of the stop the trip departs from.")]
    public required string StopId { get; init; }

    [Description("The line of the trip.")]
    public required string Line { get; init; }

    [Description("Planned departure.")]
    public required DateTimeOffset PlannedAt { get; init; }

    [Description("The guessed delay in whole minutes.")]
    public required int GuessedDelayMinutes { get; init; }
}
