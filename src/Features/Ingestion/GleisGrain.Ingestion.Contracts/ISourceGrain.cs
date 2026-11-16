namespace GleisGrain.Ingestion.Contracts;

/// <summary>
/// The one gateway to the Linz AG EFA server. Rate limiting and caching happen behind this interface,
/// so callers can ask as often as they like.
/// </summary>
/// <remarks>The grain key is the <see cref="SourceId"/> value. Use <see cref="SourceId.LinzAg"/>.</remarks>
[Alias("GleisGrain.Ingestion.Contracts.ISourceGrain")]
public interface ISourceGrain : IGrainWithStringKey
{
    /// <summary>Next departures of a stop. <paramref name="efaStopId"/> is the stop id the EFA server uses.</summary>
    [Alias("GetDeparturesAsync")]
    Task<IReadOnlyList<DepartureSnapshot>> GetDeparturesAsync(string efaStopId);

    /// <summary>All current service notices.</summary>
    [Alias("GetNoticesAsync")]
    Task<IReadOnlyList<NoticeSnapshot>> GetNoticesAsync();

    [Alias("GetStatsAsync")]
    Task<SourceStats> GetStatsAsync();
}
