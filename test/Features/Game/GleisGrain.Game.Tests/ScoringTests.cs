using GleisGrain.Game.Domain;

namespace GleisGrain.Game.Tests;

public class ScoringTests
{
    [Theory]
    [InlineData(0, 0, 3)]
    [InlineData(2, 2, 3)]
    [InlineData(15, 15, 3)]
    [InlineData(2, 3, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(0, 1, 1)]
    [InlineData(2, 4, 0)]
    [InlineData(5, 0, 0)]
    [InlineData(0, 20, 0)]
    public void Points_ForGuessAndActualDelay_FollowsTheRule(int guess, int actual, int expected) =>
        Assert.Equal(expected, Scoring.Points(guess, actual));
}
