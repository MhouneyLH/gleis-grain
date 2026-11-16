namespace GleisGrain.Trips;

/// <summary>Trips has no tunables yet: it only records what the stops report.</summary>
internal sealed class TripsOptions
{
    public const string SectionName = "Trips";

    /// <summary>Grain storage name; equals the connection string name of the trips database.</summary>
    public const string StorageName = "trips";
}
