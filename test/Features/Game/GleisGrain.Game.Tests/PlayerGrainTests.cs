using System.Text.Json;
using GleisGrain.Cluster.Contracts;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Tests;

public class PlayerGrainTests(GameCluster game) : IClassFixture<GameCluster>
{
    private IGrainFactory Grains => game.Grains;

    [Fact]
    public async Task ClaimAsync_NewNickname_CreatesThePlayer()
    {
        var nickname = Nickname.From("Max " + GameCluster.NewId()[..6]);

        var claimed = await Player(nickname).ClaimAsync(nickname);

        Assert.True(claimed);
        Assert.Equal(new PlayerSnapshot(nickname, 0, null, null), await Player(nickname).GetAsync());
    }

    [Fact]
    public async Task ClaimAsync_NicknameThatIsTaken_ReturnsFalseAndKeepsTheFirstClaim()
    {
        var first = Nickname.From("Max" + GameCluster.NewId()[..6]);
        await Player(first).ClaimAsync(first);

        var claimedAgain = await Player(first).ClaimAsync(first);

        Assert.False(claimedAgain);
    }

    [Fact]
    public async Task ClaimAsync_NicknameInOtherCase_IsTheSamePlayer()
    {
        var suffix = GameCluster.NewId()[..6];
        var first = Nickname.From("Max" + suffix);
        var shouting = Nickname.From("MAX" + suffix);
        await Player(first).ClaimAsync(first);

        var claimed = await Player(shouting).ClaimAsync(shouting);

        Assert.False(claimed);
        Assert.Equal(first, (await Player(shouting).GetAsync())?.Nickname);
    }

    [Fact]
    public async Task GetAsync_ForANicknameNobodyClaimed_ReturnsNull()
    {
        var nickname = Nickname.From("Ghost" + GameCluster.NewId()[..6]);

        Assert.Null(await Player(nickname).GetAsync());
    }

    [Fact]
    public async Task WatchStopAsync_WithoutPlayer_ReturnsNoPlayer()
    {
        var nickname = Nickname.From("Ghost" + GameCluster.NewId()[..6]);

        var outcome = await Player(nickname).WatchStopAsync(StopId.From("s" + GameCluster.NewId()));

        Assert.Equal(WatchStopOutcome.NoPlayer, outcome);
    }

    [Fact]
    public async Task WatchStopAsync_UnknownStop_ReturnsUnknownStop()
    {
        var nickname = await NewPlayerAsync();

        var outcome = await Player(nickname).WatchStopAsync(StopId.From("s" + GameCluster.NewId()));

        Assert.Equal(WatchStopOutcome.UnknownStop, outcome);
        Assert.Null((await Player(nickname).GetAsync())?.WatchedStop);
    }

    [Fact]
    public async Task WatchStopAsync_KnownStop_MakesThePlayerAWatcher()
    {
        var nickname = await NewPlayerAsync();
        var stop = await NewStopAsync();

        var outcome = await Player(nickname).WatchStopAsync(stop);

        Assert.Equal(WatchStopOutcome.Watching, outcome);
        Assert.Equal(stop, (await Player(nickname).GetAsync())?.WatchedStop);
        Assert.Equal([nickname.Value], await Watchers(stop));
    }

    [Fact]
    public async Task WatchStopAsync_OtherStop_LeavesTheOldOneAndJoinsTheNewOne()
    {
        var nickname = await NewPlayerAsync();
        var oldStop = await NewStopAsync();
        var newStop = await NewStopAsync();
        await Player(nickname).WatchStopAsync(oldStop);

        await Player(nickname).WatchStopAsync(newStop);

        Assert.Empty(await Watchers(oldStop));
        Assert.Equal([nickname.Value], await Watchers(newStop));
        Assert.Equal(newStop, (await Player(nickname).GetAsync())?.WatchedStop);
    }

    [Fact]
    public async Task WatchStopAsync_SameStopAgain_KeepsTheWatcher()
    {
        var nickname = await NewPlayerAsync();
        var stop = await NewStopAsync();
        await Player(nickname).WatchStopAsync(stop);

        await Player(nickname).WatchStopAsync(stop);

        Assert.Equal([nickname.Value], await Watchers(stop));
    }

    [Fact]
    public async Task PlaceBetAsync_WithoutPlayer_ReturnsNoPlayer()
    {
        var nickname = Nickname.From("Ghost" + GameCluster.NewId()[..6]);
        var trip = await NewTripAsync();

        var outcome = await Player(nickname).PlaceBetAsync(trip, 2);

        Assert.Equal(PlaceBetOutcome.NoPlayer, outcome);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public async Task PlaceBetAsync_GuessOutsideZeroToFifteen_ReturnsInvalidGuess(int guess)
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();

        var outcome = await Player(nickname).PlaceBetAsync(trip, guess);

        Assert.Equal(PlaceBetOutcome.InvalidGuess, outcome);
    }

    [Fact]
    public async Task PlaceBetAsync_UnknownTrip_ReturnsUnknownTrip()
    {
        var nickname = await NewPlayerAsync();

        var outcome = await Player(nickname).PlaceBetAsync(TripId.From("t" + GameCluster.NewId()), 2);

        Assert.Equal(PlaceBetOutcome.UnknownTrip, outcome);
    }

    [Fact]
    public async Task PlaceBetAsync_TripThatDeparted_ReturnsTripAlreadyDeparted()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync(hasDeparted: true);

        var outcome = await Player(nickname).PlaceBetAsync(trip, 2);

        Assert.Equal(PlaceBetOutcome.TripAlreadyDeparted, outcome);
        Assert.Null((await Player(nickname).GetAsync())?.OpenBet);
    }

    [Fact]
    public async Task PlaceBetAsync_WhileABetIsOpen_ReturnsAlreadyHasOpenBet()
    {
        var nickname = await NewPlayerAsync();
        var first = await NewTripAsync();
        var second = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(first, 2);

        var outcome = await Player(nickname).PlaceBetAsync(second, 5);

        Assert.Equal(PlaceBetOutcome.AlreadyHasOpenBet, outcome);
        Assert.Equal(first, (await Player(nickname).GetAsync())?.OpenBet?.TripId);
    }

    [Fact]
    public async Task PlaceBetAsync_ValidBet_OpensItWithTheTripDetails()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();

        var outcome = await Player(nickname).PlaceBetAsync(trip, 4);

        Assert.Equal(PlaceBetOutcome.Placed, outcome);
        var bet = (await Player(nickname).GetAsync())?.OpenBet;
        Assert.Equal(trip, bet?.TripId);
        Assert.Equal("60501720", bet?.StopId);
        Assert.Equal("1", bet?.Line);
        Assert.Equal(4, bet?.GuessedDelayMinutes);
    }

    [Fact]
    public async Task PlaceBetAsync_ValidBet_PublishesBetPlaced()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();

        await Player(nickname).PlaceBetAsync(trip, 4);

        var events = await GameCluster.EventsAsync(Grains, BetPlaced.EventType, nickname.Value);
        var payload = JsonSerializer.Deserialize<BetPlaced>(Assert.Single(events).Json, JsonSerializerOptions.Web);
        Assert.Equal(new BetPlaced(nickname.Value, "60501720", "1"), payload);
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(3, 1)]
    [InlineData(5, 0)]
    public async Task ResolveBetAsync_AfterDeparture_ScoresExactOffByOneAndMiss(int actualDelay, int expectedPoints)
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(trip, 2);
        await GameCluster.SetTripAsync(Grains, trip.Value, hasDeparted: true, delayMinutes: actualDelay);

        var resolution = await Player(nickname).ResolveBetAsync();

        Assert.Equal(new BetResolution(trip, 2, actualDelay, expectedPoints), resolution);
        var player = await Player(nickname).GetAsync();
        Assert.Equal(expectedPoints, player?.Score);
        Assert.Null(player?.OpenBet);
    }

    [Fact]
    public async Task ResolveBetAsync_BeforeDeparture_KeepsTheBetOpen()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(trip, 2);

        var resolution = await Player(nickname).ResolveBetAsync();

        Assert.Null(resolution);
        Assert.Equal(trip, (await Player(nickname).GetAsync())?.OpenBet?.TripId);
    }

    [Fact]
    public async Task ResolveBetAsync_WithoutOpenBet_ReturnsNull()
    {
        var nickname = await NewPlayerAsync();

        Assert.Null(await Player(nickname).ResolveBetAsync());
    }

    [Fact]
    public async Task ResolveBetAsync_CalledTwice_ScoresOnlyOnce()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(trip, 2);
        await GameCluster.SetTripAsync(Grains, trip.Value, hasDeparted: true, delayMinutes: 2);

        await Player(nickname).ResolveBetAsync();
        var second = await Player(nickname).ResolveBetAsync();

        Assert.Null(second);
        Assert.Equal(3, (await Player(nickname).GetAsync())?.Score);
    }

    [Fact]
    public async Task ResolveBetAsync_AfterResolving_AllowsANewBet()
    {
        var nickname = await NewPlayerAsync();
        var first = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(first, 2);
        await GameCluster.SetTripAsync(Grains, first.Value, hasDeparted: true, delayMinutes: 2);
        await Player(nickname).ResolveBetAsync();
        var second = await NewTripAsync();

        var outcome = await Player(nickname).PlaceBetAsync(second, 1);

        Assert.Equal(PlaceBetOutcome.Placed, outcome);
    }

    [Fact]
    public async Task ResolveBetAsync_AfterDeparture_PublishesBetResolved()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(trip, 2);
        await GameCluster.SetTripAsync(Grains, trip.Value, hasDeparted: true, delayMinutes: 3);

        await Player(nickname).ResolveBetAsync();

        var events = await GameCluster.EventsAsync(Grains, BetResolved.EventType, nickname.Value);
        var payload = JsonSerializer.Deserialize<BetResolved>(Assert.Single(events).Json, JsonSerializerOptions.Web);
        Assert.Equal(new BetResolved(nickname.Value, trip.Value, 3, 1), payload);
    }

    [Fact]
    public async Task ResolveBetAsync_AfterDeparture_ReportsTheScoreToTheLeaderboard()
    {
        var nickname = await NewPlayerAsync();
        var trip = await NewTripAsync();
        await Player(nickname).PlaceBetAsync(trip, 2);
        await GameCluster.SetTripAsync(Grains, trip.Value, hasDeparted: true, delayMinutes: 2);

        await Player(nickname).ResolveBetAsync();

        var top = await GameCluster.EventuallyAsync(
            () => Grains.GetGrain<ILeaderboardGrain>(LeaderboardKeys.Main).GetTopAsync(100),
            entries => entries.Any(entry => entry.Nickname == nickname.Value));
        Assert.Contains(new LeaderboardEntry(nickname.Value, 3), top);
    }

    private IPlayerGrain Player(Nickname nickname) => Grains.GetGrain<IPlayerGrain>(nickname.Key);

    private async Task<Nickname> NewPlayerAsync() => Nickname.From(await GameCluster.NewPlayerAsync(Grains));

    private async Task<StopId> NewStopAsync()
    {
        var id = "s" + GameCluster.NewId();
        await GameCluster.KnownStopAsync(Grains, id);

        return StopId.From(id);
    }

    private async Task<TripId> NewTripAsync(bool hasDeparted = false)
    {
        var id = "t" + GameCluster.NewId();
        await GameCluster.SetTripAsync(Grains, id, hasDeparted);

        return TripId.From(id);
    }

    private Task<IReadOnlyList<string>> Watchers(StopId stop) => Grains.GetGrain<IFakeStop>(stop.Value).GetWatchersAsync();
}
