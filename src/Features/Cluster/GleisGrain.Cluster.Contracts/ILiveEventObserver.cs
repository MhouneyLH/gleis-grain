using Orleans.Concurrency;

namespace GleisGrain.Cluster.Contracts;

[Alias("GleisGrain.Cluster.Contracts.ILiveEventObserver")]
public interface ILiveEventObserver : IGrainObserver
{
    [OneWay, Alias("OnEvent")]
    Task OnEvent(LiveEvent liveEvent);
}
