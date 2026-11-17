using GleisGrain.Cluster.Contracts;
using GleisGrain.Ingestion.Contracts;
using GleisGrain.Stops.Catalogue;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;
using Microsoft.Extensions.Options;

namespace GleisGrain.Stops.Domain;

// The Cluster module finds stop grains by this grain type, so keep the name.
[GrainType("stop")]
internal sealed class StopGrain(
    [PersistentState("stop", StopsOptions.StorageName)] IPersistentState<StopState> state,
    StopCatalogue catalogue,
    IOptions<StopsOptions> options,
    TimeProvider time,
    ILocalSiloDetails silo,
    StopsMetrics metrics,
    ILogger<StopGrain> logger)
    : Grain, IStopGrain, IRemindable
{
    private const string ReminderName = "refresh";

    private Stop? _stop;
    private IGrainTimer? _timer;
    private bool _reminderRegistered;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = this.GetPrimaryKeyString();
        if (!StopId.TryParse(key, null, out var id) || !catalogue.TryGet(id, out var info))
        {
            // Not a curated stop: the grain answers null and stays silent, no events for it.
            logger.UnknownStop(key);

            return Task.CompletedTask;
        }

        _stop = state.RecordExists ? state.State.ToStop(info) : new Stop(info);
        StartTimerWhenWatched(_stop);
        Publish(_stop, new StopActivated(key, silo.Name));

        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _timer = null;

        if (_stop is not null)
        {
            Publish(_stop, new StopDeactivated(_stop.Info.Id.Value));
        }

        return Task.CompletedTask;
    }

    public async Task<StopBoardSnapshot?> GetBoardAsync()
    {
        if (_stop is null)
        {
            return null;
        }

        await EnsureReminderAsync();

        if (_stop.NeedsRefresh(time.GetUtcNow(), options.Value.BoardMaxAge))
        {
            await TryRefreshAsync(_stop, "request");
        }

        return _stop.ToSnapshot();
    }

    public async Task<int> WatchAsync(string watcher)
    {
        if (_stop is null)
        {
            return 0;
        }

        if (_stop.Watch(watcher))
        {
            await SaveAsync(_stop);
            // A watched stop needs the reminder too: it brings the grain, and with it the timer, back after a silo died.
            await EnsureReminderAsync();
            StartTimerWhenWatched(_stop);
            WatchersChanged(_stop);
        }

        return _stop.Watchers.Count;
    }

    public async Task<int> UnwatchAsync(string watcher)
    {
        if (_stop is null)
        {
            return 0;
        }

        if (_stop.Unwatch(watcher))
        {
            await SaveAsync(_stop);
            StopTimerWhenUnwatched(_stop);
            WatchersChanged(_stop);
        }

        return _stop.Watchers.Count;
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (_stop is null)
        {
            return;
        }

        await TryRefreshAsync(_stop, "reminder");
        await SaveAsync(_stop);
    }

    private async Task EnsureReminderAsync()
    {
        if (_reminderRegistered)
        {
            return;
        }

        var period = options.Value.ReminderPeriod;
        await this.RegisterOrUpdateReminder(ReminderName, dueTime: period, period: period);
        _reminderRegistered = true;
    }

    private void StartTimerWhenWatched(Stop stop)
    {
        if (_timer is not null || stop.Watchers.Count == 0)
        {
            return;
        }

        var period = options.Value.WatchedRefreshPeriod;
        _timer = this.RegisterGrainTimer(
            _ => TryRefreshAsync(stop, "timer"),
            new GrainTimerCreationOptions(dueTime: period, period: period) { KeepAlive = true });
    }

    private void StopTimerWhenUnwatched(Stop stop)
    {
        if (stop.Watchers.Count > 0)
        {
            return;
        }

        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Asks the source for the departures, replaces the board and reports every departure to its trip.
    /// A failing source keeps the old board: the next trigger tries again.
    /// </summary>
    private async Task TryRefreshAsync(Stop stop, string trigger)
    {
        try
        {
            var id = stop.Info.Id.Value;
            var snapshots = await GrainFactory.GetGrain<ISourceGrain>(SourceId.LinzAg.Value).GetDeparturesAsync(id);
            var now = time.GetUtcNow();
            var departures = snapshots
                .Select(d => new Departure(TripId.For(id, d.Line, d.PlannedAt), d.Line, d.Direction, d.PlannedAt, d.DelayMinutes, d.Platform))
                .ToList();

            stop.UpdateBoard(departures, now);

            foreach (var departure in departures)
            {
                var report = new TripReport(id, departure.Line, departure.Direction, departure.PlannedAt, departure.DelayMinutes, now);
                GrainFactory.GetGrain<ITripGrain>(departure.TripId.Value).ReportAsync(report).Ignore();
            }

            metrics.BoardRefreshed(trigger, succeeded: true);
            logger.BoardRefreshed(id, trigger, departures.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            metrics.BoardRefreshed(trigger, succeeded: false);
            logger.RefreshFailed(exception, stop.Info.Id.Value, trigger);
        }
    }

    // Write-behind: watcher changes and the reminder refresh are persisted, the 15 second timer refresh is not.
    // Losing up to a minute of departures when a silo dies is fine, the next refresh brings them back.
    private async Task SaveAsync(Stop stop)
    {
        state.State = StopState.Of(stop);
        await state.WriteStateAsync();
    }

    private void WatchersChanged(Stop stop)
    {
        logger.WatchersChanged(stop.Info.Id.Value, stop.Watchers.Count);
        Publish(stop, new StopWatchersChanged(stop.Info.Id.Value, stop.Watchers.Count));
    }

    /// <summary>Fire and forget: a live event is never worth failing a grain call for.</summary>
    private void Publish<TPayload>(Stop stop, TPayload payload)
        where TPayload : ILivePayload
    {
        try
        {
            GrainFactory.GetGrain<IClusterEventsGrain>(ClusterEventKeys.Hub).PublishAsync(LiveEvent.Of(payload)).Ignore();
        }
        catch (Exception exception)
        {
            logger.PublishFailed(exception, stop.Info.Id.Value);
        }
    }
}
