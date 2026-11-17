using System.Diagnostics.Metrics;

namespace GleisGrain.Alerts;

internal sealed class AlertsMetrics
{
    public const string MeterName = "GleisGrain.Alerts";

    private readonly Counter<long> _lookups;
    private readonly Counter<long> _refreshes;

    public AlertsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _lookups = meter.CreateCounter<long>(
            "gleisgrain.alerts.lookups",
            unit: "{lookup}",
            description: "Number of alert lookups, tagged by whether a line or a stop filter was given.");
        _refreshes = meter.CreateCounter<long>(
            "gleisgrain.alerts.refreshes",
            unit: "{refresh}",
            description: "Number of refreshes of the alert board, tagged by whether the alerts changed.");
    }

    public void AlertsLookedUp(bool filtered) =>
        _lookups.Add(1, new KeyValuePair<string, object?>("filtered", filtered));

    public void BoardRefreshed(bool changed) =>
        _refreshes.Add(1, new KeyValuePair<string, object?>("changed", changed));
}
