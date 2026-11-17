using System.Collections.Concurrent;
using GleisGrain.Cluster.Contracts;
using GleisGrain.Ingestion.Contracts;
using GleisGrain.Trips.Contracts;
using Microsoft.Extensions.Time.Testing;

namespace GleisGrain.Stops.Tests.Fakes;

/// <summary>
/// What the fake grains of the other modules know and what they recorded. One instance per test,
/// shared with the fake grains through dependency injection.
/// </summary>
public sealed class StopsFakes
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<DepartureSnapshot>> _departures = new();
    private int _sourceCalls;

    // Starts at the real time: Orleans reads this clock too and takes a clock hours away for a dead cluster.
    public FakeTimeProvider Time { get; } = new(DateTimeOffset.UtcNow);

    public ConcurrentQueue<TripReport> Reports { get; } = new();

    public ConcurrentQueue<LiveEvent> Events { get; } = new();

    public int SourceCalls => Volatile.Read(ref _sourceCalls);

    /// <summary>When true the fake source throws like an unreachable Linz AG server.</summary>
    public bool SourceFails { get; set; }

    public void SetDepartures(string stopId, params DepartureSnapshot[] departures) =>
        _departures[stopId] = departures;

    public IReadOnlyList<DepartureSnapshot> GetDepartures(string stopId)
    {
        Interlocked.Increment(ref _sourceCalls);

        return SourceFails
            ? throw new HttpRequestException("Linz AG is down.")
            : _departures.GetValueOrDefault(stopId) ?? [];
    }
}

/// <summary>Stands in for the Ingestion module, which the Stops tests do not reference.</summary>
public sealed class FakeSourceGrain(StopsFakes fakes) : Grain, ISourceGrain
{
    public Task<IReadOnlyList<DepartureSnapshot>> GetDeparturesAsync(string efaStopId) =>
        Task.FromResult(fakes.GetDepartures(efaStopId));

    public Task<IReadOnlyList<NoticeSnapshot>> GetNoticesAsync() =>
        Task.FromResult<IReadOnlyList<NoticeSnapshot>>([]);

    public Task<SourceStats> GetStatsAsync() =>
        Task.FromResult(new SourceStats(0, 0, null));
}

/// <summary>Stands in for the Trips module and records every report.</summary>
public sealed class FakeTripGrain(StopsFakes fakes) : Grain, ITripGrain
{
    public Task ReportAsync(TripReport report)
    {
        fakes.Reports.Enqueue(report);

        return Task.CompletedTask;
    }

    public Task<TripSnapshot?> GetAsync() => Task.FromResult<TripSnapshot?>(null);
}

/// <summary>Stands in for the Cluster module and records every published event.</summary>
public sealed class FakeClusterEventsGrain(StopsFakes fakes) : Grain, IClusterEventsGrain
{
    public Task PublishAsync(LiveEvent liveEvent)
    {
        fakes.Events.Enqueue(liveEvent);

        return Task.CompletedTask;
    }

    public Task SubscribeAsync(ILiveEventObserver observer) => Task.CompletedTask;

    public Task UnsubscribeAsync(ILiveEventObserver observer) => Task.CompletedTask;
}
