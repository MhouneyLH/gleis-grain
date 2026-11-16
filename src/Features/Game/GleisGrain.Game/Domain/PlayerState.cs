using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>Persisted blob. Only add fields with new ids; bump <see cref="CurrentSchemaVersion"/> and migrate on activation.</summary>
[GenerateSerializer, Alias("GleisGrain.Game.Domain.PlayerState")]
internal sealed class PlayerState
{
    public const int CurrentSchemaVersion = 1;

    [Id(0)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [Id(1)]
    public string Nickname { get; set; } = string.Empty;

    [Id(2)]
    public int Score { get; set; }

    [Id(3)]
    public string? WatchedStopId { get; set; }

    [Id(4)]
    public BetState? OpenBet { get; set; }

    public static PlayerState From(Player player) => new()
    {
        Nickname = player.Nickname.Value,
        Score = player.Score,
        WatchedStopId = player.WatchedStop?.Value,
        OpenBet = player.OpenBet is { } bet ? BetState.From(bet) : null,
    };

    public Player ToPlayer() =>
        new(new Nickname(Nickname), Score, WatchedStopId is { } stop ? new StopId(stop) : null, OpenBet?.ToBet());
}

[GenerateSerializer, Alias("GleisGrain.Game.Domain.BetState")]
internal sealed class BetState
{
    [Id(0)]
    public string TripId { get; set; } = string.Empty;

    [Id(1)]
    public string StopId { get; set; } = string.Empty;

    [Id(2)]
    public string Line { get; set; } = string.Empty;

    [Id(3)]
    public DateTimeOffset PlannedAt { get; set; }

    [Id(4)]
    public int GuessedDelayMinutes { get; set; }

    public static BetState From(Bet bet) => new()
    {
        TripId = bet.TripId.Value,
        StopId = bet.StopId,
        Line = bet.Line,
        PlannedAt = bet.PlannedAt,
        GuessedDelayMinutes = bet.GuessedDelayMinutes,
    };

    public Bet ToBet() => new(new TripId(TripId), StopId, Line, PlannedAt, GuessedDelayMinutes);
}
