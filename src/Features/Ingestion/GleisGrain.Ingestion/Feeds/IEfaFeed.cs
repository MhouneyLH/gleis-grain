using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Ingestion.Feeds;

/// <summary>Where EFA answers come from, already parsed. A failed fetch surfaces as an exception.</summary>
internal interface IEfaFeed
{
    Task<IReadOnlyList<DepartureSnapshot>> FetchDeparturesAsync(string efaStopId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NoticeSnapshot>> FetchNoticesAsync(CancellationToken cancellationToken);
}
