using System.Collections.Concurrent;
using GleisGrain.Cluster.Contracts;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Storage;
using Orleans.TestingHost;

namespace GleisGrain.Game.Tests;

/// <summary>The game wired into one test silo: in-memory state and reminders, fake grains of the other modules.</summary>
public sealed class GameCluster : IAsyncLifetime
{
    private static readonly DateTimeOffset _plannedAt = new(2026, 11, 17, 10, 5, 0, TimeSpan.Zero);

    private TestCluster? _cluster;

    public IGrainFactory Grains => _cluster?.GrainFactory ?? throw new InvalidOperationException("The cluster is not started.");

    public async ValueTask InitializeAsync() => _cluster = await StartAsync<InMemorySilo>();

    public async ValueTask DisposeAsync()
    {
        if (_cluster is not null)
        {
            await _cluster.DisposeAsync();
        }
    }

    public static async Task<TestCluster> StartAsync<TConfigurator>()
        where TConfigurator : class, ISiloConfigurator, new()
    {
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<TConfigurator>();
        var cluster = builder.Build();
        await cluster.DeployAsync();

        return cluster;
    }

    public static async Task<string> NewPlayerAsync(IGrainFactory grains, string? prefix = null)
    {
        var nickname = Nickname.From($"{prefix ?? "p"}{Guid.NewGuid():N}"[..12]);
        await grains.GetGrain<IPlayerGrain>(nickname.Key).ClaimAsync(nickname);

        return nickname.Value;
    }

    /// <summary>Makes a stop known to the fake stop grain.</summary>
    public static Task KnownStopAsync(IGrainFactory grains, string stopId) =>
        grains.GetGrain<IFakeStop>(stopId).SetBoardAsync(
            new StopBoardSnapshot(StopId.From(stopId), "Stop " + stopId, 48.3, 14.3, 0, null, []));

    /// <summary>Makes a trip known to the fake trip grain.</summary>
    public static Task SetTripAsync(IGrainFactory grains, string tripId, bool hasDeparted = false, int delayMinutes = 0) =>
        grains.GetGrain<IFakeTrip>(tripId).SetAsync(
            new TripSnapshot(TripId.From(tripId), "60501720", "1", "Universität", _plannedAt, delayMinutes, hasDeparted));

    public static string NewId() => Guid.NewGuid().ToString("N");

    /// <summary>Waits until the condition holds. For one-way calls, whose effect arrives a moment after the call.</summary>
    public static async Task<T> EventuallyAsync<T>(Func<Task<T>> read, Func<T, bool> done)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, TestContext.Current.CancellationToken);

        while (true)
        {
            var value = await read();
            if (done(value) || linked.IsCancellationRequested)
            {
                return value;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), CancellationToken.None);
        }
    }

    public static Task<IReadOnlyList<LiveEvent>> EventsAsync(IGrainFactory grains, string type, string containing, int count = 1) =>
        EventuallyAsync(
            async () => (IReadOnlyList<LiveEvent>)[.. (await grains.GetGrain<IFakeEventLog>(ClusterEventKeys.Hub).GetEventsAsync())
                .Where(e => e.Type == type && e.Json.Contains(containing, StringComparison.Ordinal))],
            events => events.Count >= count);
}

public class InMemorySilo : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage(GameModule.StorageName);
        siloBuilder.UseInMemoryReminderService();
        siloBuilder.ConfigureServices(AddGame);
    }

    internal static void AddGame(IServiceCollection services)
    {
        services.AddOptions<GameOptions>();
        services.AddSingleton<GameMetrics>();
    }
}

/// <summary>Grain state in a static dictionary, so it outlives a cluster the way a database outlives a silo.</summary>
public sealed class SharedStorage(Serializer serializer) : IGrainStorage
{
    private static readonly ConcurrentDictionary<string, byte[]> _store = new();

    public Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (_store.TryGetValue(Key(stateName, grainId), out var bytes))
        {
            grainState.State = serializer.Deserialize<T>(bytes);
            grainState.RecordExists = true;
        }

        return Task.CompletedTask;
    }

    public Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        _store[Key(stateName, grainId)] = serializer.SerializeToArray(grainState.State);
        grainState.RecordExists = true;

        return Task.CompletedTask;
    }

    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        _store.TryRemove(Key(stateName, grainId), out _);
        grainState.RecordExists = false;

        return Task.CompletedTask;
    }

    private static string Key(string stateName, GrainId grainId) => $"{stateName}/{grainId}";
}

public class SharedStorageSilo : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddKeyedSingleton<IGrainStorage>(GameModule.StorageName, (sp, _) => new SharedStorage(sp.GetRequiredService<Serializer>()));
        siloBuilder.UseInMemoryReminderService();
        siloBuilder.ConfigureServices(InMemorySilo.AddGame);
    }
}
