using GleisGrain.Cluster.Contracts;

namespace GleisGrain.Game.Domain;

internal static class FireAndForget
{
    /// <summary>Publishes a live event. A failing publish is logged and never fails the caller.</summary>
    public static Task PublishAsync<TPayload>(this IGrainFactory grains, ILogger logger, TPayload payload)
        where TPayload : ILivePayload =>
        RunAsync(logger, TPayload.EventType, () =>
            grains.GetGrain<IClusterEventsGrain>(ClusterEventKeys.Hub).PublishAsync(LiveEvent.Of(payload)));

    /// <summary>Runs a call whose result nobody waits for. A failure is logged and never fails the caller.</summary>
    public static async Task RunAsync(ILogger logger, string call, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            logger.FireAndForgetFailed(exception, call);
        }
    }
}
