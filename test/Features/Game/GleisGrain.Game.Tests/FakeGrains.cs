using GleisGrain.Stops.Contracts;

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
