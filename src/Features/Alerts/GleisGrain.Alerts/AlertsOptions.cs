using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Alerts;

internal sealed class AlertsOptions
{
    public const string SectionName = "Alerts";

    /// <summary>Grain storage name; equals the connection string name of the alerts database.</summary>
    public const string StorageName = "alerts";

    /// <summary>How often the reminder refreshes the board in the background. Orleans does not allow reminders below one minute.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan ReminderPeriod { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>A request refreshes the board first when the last refresh is older than this.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan StaleAfter { get; init; } = TimeSpan.FromMinutes(2);
}
