using GleisGrain.Game.Domain;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Tests;

public class BetTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(15)]
    public void IsValidGuess_WithinZeroToFifteen_IsTrue(int minutes) =>
        Assert.True(Bet.IsValidGuess(minutes));

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    [InlineData(int.MaxValue)]
    public void IsValidGuess_OutsideZeroToFifteen_IsFalse(int minutes) =>
        Assert.False(Bet.IsValidGuess(minutes));

    [Fact]
    public void Constructor_WithInvalidGuess_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Bet(TripId.From("stop:1:202611171005"), "stop", "1", DateTimeOffset.UnixEpoch, 16));
}
