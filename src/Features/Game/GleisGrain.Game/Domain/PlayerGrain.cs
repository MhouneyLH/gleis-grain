using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Domain;

internal sealed class PlayerGrain(
    [PersistentState("player", GameOptions.StorageName)] IPersistentState<PlayerState> state,
    GameMetrics metrics,
    ILogger<PlayerGrain> logger)
    : Grain, IPlayerGrain
{
    private Player? _player;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _player = state.RecordExists ? state.State.ToPlayer() : null;

        return Task.CompletedTask;
    }

    public async Task<bool> ClaimAsync(Nickname nickname)
    {
        if (nickname.Key != this.GetPrimaryKeyString())
        {
            throw new ArgumentException($"Nickname '{nickname}' does not belong to grain '{this.GetPrimaryKeyString()}'.", nameof(nickname));
        }

        if (_player is not null)
        {
            return false;
        }

        _player = new Player(nickname);
        await SaveAsync(_player);

        metrics.PlayerClaimed();
        logger.PlayerClaimed(nickname);

        return true;
    }

    public Task<PlayerSnapshot?> GetAsync() => Task.FromResult(_player?.ToSnapshot());

    public async Task<WatchStopOutcome> WatchStopAsync(StopId stopId)
    {
        if (_player is not { } player)
        {
            return WatchStopOutcome.NoPlayer;
        }

        var stop = GrainFactory.GetGrain<IStopGrain>(stopId.Value);
        if (await stop.GetBoardAsync() is null)
        {
            return WatchStopOutcome.UnknownStop;
        }

        if (player.WatchedStop is { } previous && previous != stopId)
        {
            await GrainFactory.GetGrain<IStopGrain>(previous.Value).UnwatchAsync(player.Nickname.Value);
        }

        await stop.WatchAsync(player.Nickname.Value);
        player.Watch(stopId);
        await SaveAsync(player);

        logger.PlayerWatchesStop(player.Nickname, stopId);

        return WatchStopOutcome.Watching;
    }

    private async Task SaveAsync(Player player)
    {
        state.State = PlayerState.From(player);
        await state.WriteStateAsync();
    }
}
