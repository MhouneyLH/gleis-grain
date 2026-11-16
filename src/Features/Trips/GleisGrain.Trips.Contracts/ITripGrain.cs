using Orleans.Concurrency;

namespace GleisGrain.Trips.Contracts;

/// <summary>
/// One departure of a line at a stop with its delay. Stops report what they see, others read the result.
/// </summary>
/// <remarks>The grain key is the <see cref="TripId"/> value.</remarks>
[Alias("GleisGrain.Trips.Contracts.ITripGrain")]
public interface ITripGrain : IGrainWithStringKey
{
    /// <summary>Records the latest observed delay. Fire and forget: losing one report is fine, the next refresh brings another.</summary>
    [OneWay, Alias("ReportAsync")]
    Task ReportAsync(TripReport report);

    /// <summary>The trip, or null when nobody has reported it yet.</summary>
    [Alias("GetAsync")]
    Task<TripSnapshot?> GetAsync();
}
