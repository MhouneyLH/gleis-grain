using Microsoft.Extensions.Configuration;
using Orleans.TestingHost;

namespace GleisGrain.Stops.Tests.Fakes;

/// <summary>One silo with the Stops module, in-memory storage and the fake grains of the other modules.</summary>
internal sealed class StopsCluster : IAsyncDisposable
{
    private readonly string _key;
    private readonly TestCluster _cluster;

    private StopsCluster(string key, StopsFakes fakes, TestCluster cluster)
    {
        _key = key;
        Fakes = fakes;
        _cluster = cluster;
    }

    public StopsFakes Fakes { get; }

    public IGrainFactory Grains => _cluster.Client;

    /// <param name="settings">Overrides for the <c>Stops</c> options section, e.g. <c>UseBoardCache</c>.</param>
    public static async Task<StopsCluster> StartAsync(IReadOnlyDictionary<string, string?>? settings = null)
    {
        var fakes = new StopsFakes();
        var key = Guid.NewGuid().ToString("N");
        StopsSiloConfigurator.Registry[key] = fakes;

        var configuration = new Dictionary<string, string?> { [StopsSiloConfigurator.KeySetting] = key };
        foreach (var (name, value) in settings ?? new Dictionary<string, string?>())
        {
            configuration[$"{StopsOptions.SectionName}:{name}"] = value;
        }

        var builder = new TestClusterBuilder(1);
        builder.ConfigureHostConfiguration(host => host.AddInMemoryCollection(configuration));
        builder.AddSiloBuilderConfigurator<StopsSiloConfigurator>();

        var cluster = builder.Build();
        await cluster.DeployAsync();

        return new StopsCluster(key, fakes, cluster);
    }

    public async ValueTask DisposeAsync()
    {
        await _cluster.StopAllSilosAsync();
        await _cluster.DisposeAsync();
        StopsSiloConfigurator.Registry.TryRemove(_key, out _);
    }

    /// <summary>Calls to other grains are one-way, so their effect shows up shortly after the call returns.</summary>
    public static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        while (!condition())
        {
            await Task.Delay(20, timeout.Token);
        }
    }
}
