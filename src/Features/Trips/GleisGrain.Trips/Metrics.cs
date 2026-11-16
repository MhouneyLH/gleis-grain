using System.Diagnostics.Metrics;

namespace GleisGrain.Trips;

internal sealed class TripsMetrics
{
    public const string MeterName = "GleisGrain.Trips";

    private readonly Counter<long> _tripLookups;
    private readonly Counter<long> _reports;

    public TripsMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _tripLookups = meter.CreateCounter<long>(
            "gleisgrain.trips.trip.lookups",
            unit: "{lookup}",
            description: "Number of trip lookups, tagged by whether the trip was found.");
        _reports = meter.CreateCounter<long>(
            "gleisgrain.trips.report.received",
            unit: "{report}",
            description: "Number of delay reports from stops, tagged by whether they changed the delay.");
    }

    public void TripLookedUp(bool found) =>
        _tripLookups.Add(1, new KeyValuePair<string, object?>("found", found));

    public void ReportReceived(bool delayChanged) =>
        _reports.Add(1, new KeyValuePair<string, object?>("delay_changed", delayChanged));
}
