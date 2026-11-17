using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops.Domain;

/// <summary>Reads a stop board through a short-lived cache. The grain key is the stop id.</summary>
[Alias("GleisGrain.Stops.Domain.IStopBoardCacheGrain")]
internal interface IStopBoardCacheGrain : IGrainWithStringKey
{
    [Alias("GetBoardAsync")]
    Task<StopBoardSnapshot?> GetBoardAsync();
}
