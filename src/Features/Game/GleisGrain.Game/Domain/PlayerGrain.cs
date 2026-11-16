using GleisGrain.Cluster.Contracts;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;
using Microsoft.Extensions.Options;

namespace GleisGrain.Game.Domain;

internal sealed class PlayerGrain(
    [PersistentState("player", GameOptions.StorageName)] IPersistentState<PlayerState> state,
    IOptions<GameOptions> options,
    GameMetrics metrics,
    ILogger<PlayerGrain> logger)
    : Grain, IPlayerGrain, IRemindable
{
    private const string ResolveReminder = "resolve-bet";

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

    public async Task<PlaceBetOutcome> PlaceBetAsync(TripId tripId, int guessedDelayMinutes)
    {
        if (_player is not { } player)
        {
            return PlaceBetOutcome.NoPlayer;
        }

        if (!Bet.IsValidGuess(guessedDelayMinutes))
        {
            return PlaceBetOutcome.InvalidGuess;
        }

        var trip = await GrainFactory.GetGrain<ITripGrain>(tripId.Value).GetAsync();
        if (trip is null)
        {
            return PlaceBetOutcome.UnknownTrip;
        }

        if (trip.HasDeparted)
        {
            return PlaceBetOutcome.TripAlreadyDeparted;
        }

        var bet = new Bet(tripId, trip.StopId, trip.Line, trip.PlannedAt, guessedDelayMinutes);
        if (!player.TryPlaceBet(bet))
        {
            return PlaceBetOutcome.AlreadyHasOpenBet;
        }

        await SaveAsync(player);
        await this.RegisterOrUpdateReminder(ResolveReminder, options.Value.ResolvePeriod, options.Value.ResolvePeriod);

        metrics.BetPlaced();
        logger.BetPlaced(player.Nickname, guessedDelayMinutes, tripId);
        await GrainFactory.PublishAsync(logger, new BetPlaced(player.Nickname.Value, trip.StopId, trip.Line));

        return PlaceBetOutcome.Placed;
    }

    public async Task<BetResolution?> ResolveBetAsync()
    {
        if (_player is not { } player)
        {
            return null;
        }

        if (player.OpenBet is not { } bet)
        {
            await UnregisterResolveReminderAsync();

            return null;
        }

        if (await GrainFactory.GetGrain<ITripGrain>(bet.TripId.Value).GetAsync() is not { HasDeparted: true } trip)
        {
            return null;
        }

        var resolution = player.Resolve(trip.DelayMinutes)!;
        await SaveAsync(player);
        await UnregisterResolveReminderAsync();

        metrics.BetResolved(resolution.Points);
        logger.BetResolved(player.Nickname, resolution.Points, resolution.TripId);
        await FireAndForget.RunAsync(
            logger,
            nameof(ILeaderboardGrain.ReportScoreAsync),
            () => GrainFactory.GetGrain<ILeaderboardGrain>(LeaderboardKeys.Main).ReportScoreAsync(player.Nickname.Value, player.Score));
        await GrainFactory.PublishAsync(
            logger,
            new BetResolved(player.Nickname.Value, resolution.TripId.Value, resolution.ActualDelayMinutes, resolution.Points));

        return resolution;
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status) => await ResolveBetAsync();

    private async Task SaveAsync(Player player)
    {
        state.State = PlayerState.From(player);
        await state.WriteStateAsync();
    }

    private async Task UnregisterResolveReminderAsync()
    {
        if (await this.GetReminder(ResolveReminder) is { } reminder)
        {
            await this.UnregisterReminder(reminder);
        }
    }
}
