namespace GleisGrain.Alerts.Contracts;

/// <summary>All current service notices, refreshed in the background.</summary>
/// <remarks>The grain key is <see cref="AlertBoardKeys.Linz"/>.</remarks>
[Alias("GleisGrain.Alerts.Contracts.IAlertBoardGrain")]
public interface IAlertBoardGrain : IGrainWithStringKey
{
    /// <summary>Current notices, optionally only those naming <paramref name="line"/> or <paramref name="stopId"/>.</summary>
    [Alias("GetAsync")]
    Task<IReadOnlyList<AlertSnapshot>> GetAsync(string? line, string? stopId);
}

public static class AlertBoardKeys
{
    public const string Linz = "linz";
}
