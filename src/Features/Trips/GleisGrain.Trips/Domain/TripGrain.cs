using GleisGrain.Trips.Contracts;

namespace GleisGrain.Trips.Domain;

internal sealed class TripGrain(
    [PersistentState("trip", TripsOptions.StorageName)] IPersistentState<TripState> state,
    TimeProvider time,
    TripsMetrics metrics,
    ILogger<TripGrain> logger)
    : Grain, ITripGrain
{
    private TripId _id;
    private Trip? _trip;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _id = TripId.From(this.GetPrimaryKeyString());
        _trip = state.RecordExists ? state.State.ToTrip(_id) : null;

        return Task.CompletedTask;
    }

    public async Task ReportAsync(TripReport report)
    {
        if (_trip is null)
        {
            _trip = Trip.Start(_id, report);
            await SaveAsync(_trip);
            metrics.ReportReceived(delayChanged: true);
            logger.TripStarted(_id);

            return;
        }

        var delayChanged = _trip.Report(report);
        if (delayChanged)
        {
            await SaveAsync(_trip);
        }

        metrics.ReportReceived(delayChanged);
        logger.ReportHandled(_id, delayChanged);
    }

    public Task<TripSnapshot?> GetAsync() =>
        Task.FromResult(_trip?.ToSnapshot(time.GetUtcNow()));

    private async Task SaveAsync(Trip trip)
    {
        state.State = TripState.Of(trip);
        await state.WriteStateAsync();
    }
}
