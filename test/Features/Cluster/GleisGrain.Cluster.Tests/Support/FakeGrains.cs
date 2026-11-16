using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Cluster.Tests.Support;

// Stand-ins for grains of other modules. Cluster must not reference those modules, so the tests bring their own.

/// <summary>Stands in for the stop grain of the Stops module, which uses the same grain type name.</summary>
[Alias("GleisGrain.Cluster.Tests.Support.IFakeStopGrain")]
public interface IFakeStopGrain : IGrainWithStringKey
{
    [Alias("PingAsync")]
    Task PingAsync();
}

[GrainType("stop")]
public sealed class FakeStopGrain : Grain, IFakeStopGrain
{
    public Task PingAsync() => Task.CompletedTask;
}

public sealed class FakeSourceGrain : Grain, ISourceGrain
{
    public const long Calls = 3;
    public const long CacheHits = 7;

    public Task<IReadOnlyList<DepartureSnapshot>> GetDeparturesAsync(string efaStopId) =>
        Task.FromResult<IReadOnlyList<DepartureSnapshot>>([]);

    public Task<IReadOnlyList<NoticeSnapshot>> GetNoticesAsync() =>
        Task.FromResult<IReadOnlyList<NoticeSnapshot>>([]);

    public Task<SourceStats> GetStatsAsync() =>
        Task.FromResult(new SourceStats(Calls, CacheHits, null));
}
