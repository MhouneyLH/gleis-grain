using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

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
        Assert.Equal(new PlayerSnapshot(nickname, null), await Player(nickname).GetAsync());
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

    private IPlayerGrain Player(Nickname nickname) => Grains.GetGrain<IPlayerGrain>(nickname.Key);

    private async Task<Nickname> NewPlayerAsync() => Nickname.From(await GameCluster.NewPlayerAsync(Grains));

    private async Task<StopId> NewStopAsync()
    {
        var id = "s" + GameCluster.NewId();
        await GameCluster.KnownStopAsync(Grains, id);

        return StopId.From(id);
    }

    private Task<IReadOnlyList<string>> Watchers(StopId stop) => Grains.GetGrain<IFakeStop>(stop.Value).GetWatchersAsync();
}
