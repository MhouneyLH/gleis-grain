using System.Collections.Concurrent;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;
using Orleans.Storage;
using Orleans.TestingHost;

namespace GleisGrain.Game.Tests;

/// <summary>The game wired into one test silo: in-memory state and reminders, fake grains of the other modules.</summary>
public sealed class GameCluster : IAsyncLifetime
{
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

    public static string NewId() => Guid.NewGuid().ToString("N");
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
