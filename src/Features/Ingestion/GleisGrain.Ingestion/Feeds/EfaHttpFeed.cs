using GleisGrain.Ingestion.Contracts;
using GleisGrain.Ingestion.Domain;

namespace GleisGrain.Ingestion.Feeds;

/// <summary>Asks the Linz AG EFA server. Base address, timeout and user agent are set where the typed client is registered.</summary>
internal sealed class EfaHttpFeed(HttpClient http) : IEfaFeed
{
    private const int DepartureLimit = 20;

    public async Task<IReadOnlyList<DepartureSnapshot>> FetchDeparturesAsync(string efaStopId, CancellationToken cancellationToken)
    {
        var xml = await http.GetStringAsync(
            $"XML_DM_REQUEST?outputFormat=XML&type_dm=stop&name_dm={Uri.EscapeDataString(efaStopId)}&mode=direct&useRealtime=1&limit={DepartureLimit}",
            cancellationToken);

        return EfaParser.ParseDepartures(xml).Departures;
    }

    /// <remarks>A trip request without parameters is the cheapest way to get the notices.</remarks>
    public async Task<IReadOnlyList<NoticeSnapshot>> FetchNoticesAsync(CancellationToken cancellationToken) =>
        EfaParser.ParseNotices(await http.GetStringAsync("XML_TRIP_REQUEST2", cancellationToken));
}
