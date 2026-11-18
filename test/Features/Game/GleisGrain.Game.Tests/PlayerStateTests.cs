using GleisGrain.Game.Contracts;
using GleisGrain.Game.Domain;
using GleisGrain.Stops.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;

namespace GleisGrain.Game.Tests;

/// <summary>Persisted state is an API: a blob written while bets existed (ids 2 and 4) must still load.</summary>
public class PlayerStateTests
{
    [Fact]
    public void Deserialize_BlobWithRemovedScoreAndOpenBet_LoadsTheRemainingFields()
    {
        using var services = new ServiceCollection().AddSerializer().BuildServiceProvider();
        var serializer = services.GetRequiredService<Serializer>();
        var bytes = serializer.SerializeToArray(new OldPlayerState
        {
            Nickname = "Max",
            Score = 7,
            WatchedStopId = "60501720",
            OpenBet = new OldBetState { TripId = "t", Line = "1", GuessedDelayMinutes = 2 },
        });

        var state = serializer.Deserialize<PlayerState>(bytes)!;

        var player = state.ToPlayer();
        Assert.Equal(Nickname.From("Max"), player.Nickname);
        Assert.Equal(StopId.From("60501720"), player.WatchedStop);
        Assert.Equal(1, state.SchemaVersion);
    }
}

// The shape PlayerState had before the bets were removed. Same ids, so the bytes are what the old version stored.
[GenerateSerializer]
public sealed class OldPlayerState
{
    [Id(0)]
    public int SchemaVersion { get; set; } = 1;

    [Id(1)]
    public string Nickname { get; set; } = string.Empty;

    [Id(2)]
    public int Score { get; set; }

    [Id(3)]
    public string? WatchedStopId { get; set; }

    [Id(4)]
    public OldBetState? OpenBet { get; set; }
}

[GenerateSerializer]
public sealed class OldBetState
{
    [Id(0)]
    public string TripId { get; set; } = string.Empty;

    [Id(2)]
    public string Line { get; set; } = string.Empty;

    [Id(4)]
    public int GuessedDelayMinutes { get; set; }
}
