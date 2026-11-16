namespace GleisGrain.Ingestion.Contracts;

/// <summary>How often the source went out to Linz AG versus how often the cache answered.</summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Ingestion.Contracts.SourceStats")]
public sealed record SourceStats(
    [property: Id(0)] long Calls,
    [property: Id(1)] long CacheHits,
    [property: Id(2)] DateTimeOffset? LastCallAt);
