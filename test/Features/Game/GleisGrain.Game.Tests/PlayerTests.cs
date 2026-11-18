using GleisGrain.Game.Contracts;
using GleisGrain.Game.Domain;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Tests;

public class PlayerTests
{
    [Fact]
    public void NewPlayer_HasNoWatchedStop()
    {
        var player = new Player(Nickname.From("Max"));

        Assert.Equal(new PlayerSnapshot(Nickname.From("Max"), null), player.ToSnapshot());
    }

    [Fact]
    public void Watch_SetsTheWatchedStop()
    {
        var player = new Player(Nickname.From("Max"));

        player.Watch(StopId.From("60501720"));

        Assert.Equal(StopId.From("60501720"), player.WatchedStop);
    }
}
