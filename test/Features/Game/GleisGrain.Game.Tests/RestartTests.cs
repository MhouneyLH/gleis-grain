using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Tests;

/// <summary>A player is state in storage, not in memory: it has to survive the silo that took it.</summary>
public class RestartTests
{
    [Fact]
    public async Task WatchedStop_AfterASiloRestart_IsStillThere()
    {
        var nickname = Nickname.From("Rex" + GameCluster.NewId()[..6]);
        var stop = StopId.From("s" + GameCluster.NewId());

        await using (var first = await GameCluster.StartAsync<SharedStorageSilo>())
        {
            await GameCluster.KnownStopAsync(first.GrainFactory, stop.Value);
            var player = first.GrainFactory.GetGrain<IPlayerGrain>(nickname.Key);
            await player.ClaimAsync(nickname);
            await player.WatchStopAsync(stop);
            await first.StopAllSilosAsync(TestContext.Current.CancellationToken);
        }

        await using var second = await GameCluster.StartAsync<SharedStorageSilo>();
        var snapshot = await second.GrainFactory.GetGrain<IPlayerGrain>(nickname.Key).GetAsync();

        Assert.Equal(new PlayerSnapshot(nickname, stop), snapshot);
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
