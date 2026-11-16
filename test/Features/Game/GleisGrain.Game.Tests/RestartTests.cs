using GleisGrain.Game.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Tests;

/// <summary>A bet is state in storage, not in memory: it has to survive the silo that took it.</summary>
public class RestartTests
{
    [Fact]
    public async Task OpenBetAndScore_AfterASiloRestart_AreStillThere()
    {
        var nickname = Nickname.From("Rex" + GameCluster.NewId()[..6]);
        var scoredTrip = TripId.From("t" + GameCluster.NewId());
        var openTrip = TripId.From("t" + GameCluster.NewId());

        await using (var first = await GameCluster.StartAsync<SharedStorageSilo>())
        {
            var player = first.GrainFactory.GetGrain<IPlayerGrain>(nickname.Key);
            await player.ClaimAsync(nickname);
            await GameCluster.SetTripAsync(first.GrainFactory, scoredTrip.Value);
            await player.PlaceBetAsync(scoredTrip, 2);
            await GameCluster.SetTripAsync(first.GrainFactory, scoredTrip.Value, hasDeparted: true, delayMinutes: 2);
            await player.ResolveBetAsync();
            await GameCluster.SetTripAsync(first.GrainFactory, openTrip.Value);
            await player.PlaceBetAsync(openTrip, 6);
            await first.StopAllSilosAsync(TestContext.Current.CancellationToken);
        }

        await using var second = await GameCluster.StartAsync<SharedStorageSilo>();
        var restarted = second.GrainFactory.GetGrain<IPlayerGrain>(nickname.Key);
        var snapshot = await restarted.GetAsync();

        Assert.Equal(3, snapshot?.Score);
        Assert.Equal(openTrip, snapshot?.OpenBet?.TripId);
        Assert.Equal(6, snapshot?.OpenBet?.GuessedDelayMinutes);

        await GameCluster.SetTripAsync(second.GrainFactory, openTrip.Value, hasDeparted: true, delayMinutes: 6);
        var resolution = await restarted.ResolveBetAsync();

        Assert.Equal(3, resolution?.Points);
        Assert.Equal(6, (await restarted.GetAsync())?.Score);
    }

    [Fact]
    public async Task ClaimAsync_AfterASiloRestart_StillRejectsTheTakenNickname()
    {
        var nickname = Nickname.From("Ann" + GameCluster.NewId()[..6]);

        await using (var first = await GameCluster.StartAsync<SharedStorageSilo>())
        {
            await first.GrainFactory.GetGrain<IPlayerGrain>(nickname.Key).ClaimAsync(nickname);
            await first.StopAllSilosAsync(TestContext.Current.CancellationToken);
        }

        await using var second = await GameCluster.StartAsync<SharedStorageSilo>();

        Assert.False(await second.GrainFactory.GetGrain<IPlayerGrain>(nickname.Key).ClaimAsync(nickname));
    }
}
