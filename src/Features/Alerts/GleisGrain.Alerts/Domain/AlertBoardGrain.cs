using GleisGrain.Alerts.Contracts;
using GleisGrain.Ingestion.Contracts;
using Microsoft.Extensions.Options;

namespace GleisGrain.Alerts.Domain;

/// <summary>
/// Thin wrapper around <see cref="AlertBoard"/>: asks the source, calls the domain, persists when the alerts
/// changed. A request refreshes a stale board first, a reminder refreshes it in the background.
/// </summary>
internal sealed class AlertBoardGrain(
    [PersistentState("alerts", AlertsOptions.StorageName)] IPersistentState<AlertBoardState> state,
    IOptions<AlertsOptions> options,
    TimeProvider timeProvider,
    AlertsMetrics metrics,
    ILogger<AlertBoardGrain> logger)
    : Grain, IAlertBoardGrain, IRemindable
{
    private const string ReminderName = "refresh";

    private AlertBoard _board = new();

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _board = state.State.ToBoard();

        // Registering again is harmless; the first activation of the board is what starts the background refresh.
        await this.RegisterOrUpdateReminder(ReminderName, options.Value.ReminderPeriod, options.Value.ReminderPeriod, cancellationToken);
    }

    public async Task<IReadOnlyList<AlertSnapshot>> GetAsync(string? line, string? stopId)
    {
        logger.LookingUpAlerts(line, stopId);
        metrics.AlertsLookedUp(!string.IsNullOrWhiteSpace(line) || !string.IsNullOrWhiteSpace(stopId));

        if (_board.NeedsRefresh(timeProvider.GetUtcNow(), options.Value.StaleAfter))
        {
            await TryRefreshAsync();
        }

        return _board.Filter(line, stopId).Select(alert => alert.ToSnapshot()).ToList();
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status) => await TryRefreshAsync();

    /// <summary>A failed refresh is not an error as long as there is something to show. Without any data it is.</summary>
    private async Task TryRefreshAsync()
    {
        try
        {
            var notices = await GrainFactory.GetGrain<ISourceGrain>(SourceId.LinzAg.Value).GetNoticesAsync();
            var changed = _board.Refresh(notices, timeProvider.GetUtcNow());

            logger.BoardRefreshed(_board.Alerts.Count, changed);
            metrics.BoardRefreshed(changed);

            // Only changes are written; a refresh without news costs no database round trip.
            if (changed || !state.RecordExists)
            {
                state.State = AlertBoardState.From(_board);
                await state.WriteStateAsync();
            }
        }
        catch (Exception exception) when (state.RecordExists)
        {
            logger.RefreshFailed(exception);
        }
        catch (Exception exception)
        {
            logger.Unavailable(exception);
            throw;
        }
    }
}
