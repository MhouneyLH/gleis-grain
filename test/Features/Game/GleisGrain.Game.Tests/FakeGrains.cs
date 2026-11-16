using GleisGrain.Cluster.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Tests;

// Stand-ins for the grains of the other modules. The Game module only knows their Contracts, so the tests
// register these fakes in the silo. Each fake also has a control interface the test calls to arrange the scene.

[Alias("GleisGrain.Game.Tests.IFakeStop")]
public interface IFakeStop : IGrainWithStringKey
{
    /// <summary>Makes the stop known, or unknown again with null.</summary>
    [Alias("SetBoardAsync")]
    Task SetBoardAsync(StopBoardSnapshot? board);

    [Alias("GetWatchersAsync")]
    Task<IReadOnlyList<string>> GetWatchersAsync();
}

public sealed class FakeStopGrain : Grain, IStopGrain, IFakeStop
{
    private readonly HashSet<string> _watchers = [];
    private StopBoardSnapshot? _board;

    public Task SetBoardAsync(StopBoardSnapshot? board)
    {
        _board = board;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetWatchersAsync() => Task.FromResult<IReadOnlyList<string>>([.. _watchers]);

    public Task<StopBoardSnapshot?> GetBoardAsync() => Task.FromResult(_board);

    public Task<int> WatchAsync(string watcher)
    {
        _watchers.Add(watcher);

        return Task.FromResult(_watchers.Count);
    }

    public Task<int> UnwatchAsync(string watcher)
    {
        _watchers.Remove(watcher);

        return Task.FromResult(_watchers.Count);
    }
}

[Alias("GleisGrain.Game.Tests.IFakeTrip")]
public interface IFakeTrip : IGrainWithStringKey
{
    /// <summary>Sets what the trip answers, or makes it unknown with null.</summary>
    [Alias("SetAsync")]
    Task SetAsync(TripSnapshot? snapshot);
}

public sealed class FakeTripGrain : Grain, ITripGrain, IFakeTrip
{
    private TripSnapshot? _snapshot;

    public Task SetAsync(TripSnapshot? snapshot)
    {
        _snapshot = snapshot;

        return Task.CompletedTask;
    }

    public Task ReportAsync(TripReport report) => Task.CompletedTask;

    public Task<TripSnapshot?> GetAsync() => Task.FromResult(_snapshot);
}

[Alias("GleisGrain.Game.Tests.IFakeEventLog")]
public interface IFakeEventLog : IGrainWithStringKey
{
    [Alias("GetEventsAsync")]
    Task<IReadOnlyList<LiveEvent>> GetEventsAsync();
}

public sealed class FakeClusterEventsGrain : Grain, IClusterEventsGrain, IFakeEventLog
{
    private readonly List<LiveEvent> _events = [];

    public Task PublishAsync(LiveEvent liveEvent)
    {
        _events.Add(liveEvent);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LiveEvent>> GetEventsAsync() => Task.FromResult<IReadOnlyList<LiveEvent>>([.. _events]);

    public Task SubscribeAsync(ILiveEventObserver observer) => Task.CompletedTask;

    public Task UnsubscribeAsync(ILiveEventObserver observer) => Task.CompletedTask;
}
