using GleisGrain.Game.Contracts;
using GleisGrain.Game.Domain;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Tests;

public class PlayerTests
{
    private static readonly TripId _trip = TripId.From("60501720:1:202611171005");
    private static readonly DateTimeOffset _plannedAt = new(2026, 11, 17, 10, 5, 0, TimeSpan.Zero);

    [Fact]
    public void NewPlayer_HasNoScoreStopOrBet()
    {
        var player = new Player(Nickname.From("Max"));

        Assert.Equal(new PlayerSnapshot(Nickname.From("Max"), 0, null, null), player.ToSnapshot());
    }

    [Fact]
    public void Watch_SetsTheWatchedStop()
    {
        var player = new Player(Nickname.From("Max"));

        player.Watch(StopId.From("60501720"));

        Assert.Equal(StopId.From("60501720"), player.WatchedStop);
    }

    [Fact]
    public void TryPlaceBet_WithoutOpenBet_OpensIt()
    {
        var player = new Player(Nickname.From("Max"));

        var placed = player.TryPlaceBet(BetOf(2));

        Assert.True(placed);
        Assert.Equal(new OpenBet(_trip, "60501720", "1", _plannedAt, 2), player.ToSnapshot().OpenBet);
    }

    [Fact]
    public void TryPlaceBet_WithOpenBet_KeepsTheFirstOne()
    {
        var player = new Player(Nickname.From("Max"), openBet: BetOf(2));

        var placed = player.TryPlaceBet(BetOf(9));

        Assert.False(placed);
        Assert.Equal(2, player.OpenBet?.GuessedDelayMinutes);
    }

    [Theory]
    [InlineData(2, 2, 3)]
    [InlineData(2, 3, 1)]
    [InlineData(2, 6, 0)]
    public void Resolve_ScoresTheBetAndClearsIt(int guess, int actual, int points)
    {
        var player = new Player(Nickname.From("Max"), score: 4, openBet: BetOf(guess));

        var resolution = player.Resolve(actual);

        Assert.Equal(new BetResolution(_trip, guess, actual, points), resolution);
        Assert.Equal(4 + points, player.Score);
        Assert.Null(player.OpenBet);
    }

    [Fact]
    public void Resolve_WithoutOpenBet_ChangesNothing()
    {
        var player = new Player(Nickname.From("Max"), score: 4);

        var resolution = player.Resolve(2);

        Assert.Null(resolution);
        Assert.Equal(4, player.Score);
    }

    [Fact]
    public void TryPlaceBet_AfterResolve_IsAllowedAgain()
    {
        var player = new Player(Nickname.From("Max"), openBet: BetOf(2));
        player.Resolve(2);

        Assert.True(player.TryPlaceBet(BetOf(3)));
    }

    private static Bet BetOf(int guess) => new(_trip, "60501720", "1", _plannedAt, guess);
}
