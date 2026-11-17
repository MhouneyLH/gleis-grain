using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Stops;

internal sealed class StopsOptions
{
    public const string SectionName = "Stops";

    /// <summary>Grain storage name; equals the connection string name of the stops database.</summary>
    public const string StorageName = "stops";

    /// <summary>How often the reminder refreshes a board in the background. Orleans does not accept reminders below one minute.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan ReminderPeriod { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>How often the grain refreshes its board while somebody watches the stop.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan WatchedRefreshPeriod { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>A board older than this is refreshed before it is served.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan BoardMaxAge { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Serve boards through the stateless <c>StopBoardCacheGrain</c> instead of asking the stop grain every time.</summary>
    public bool UseBoardCache { get; init; }

    /// <summary>How long a cache grain serves the same snapshot.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:10:00")]
    public TimeSpan BoardCacheTtl { get; init; } = TimeSpan.FromSeconds(5);
}
