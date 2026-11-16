using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Orleans.TestingHost;

namespace GleisGrain.Trips.Tests;

/// <summary>
/// Configures the silos of a test cluster. The cluster builds its silos from the type name, so the fake clock
/// reaches them through a static registry; the test passes the registry key as a configuration setting.
/// </summary>
public sealed class TripsSiloConfigurator : ISiloConfigurator
{
    public const string KeySetting = "Tests:ClockKey";

    public static ConcurrentDictionary<string, FakeTimeProvider> Clocks { get; } = new();

    public void Configure(ISiloBuilder siloBuilder)
    {
        var clock = Clocks[siloBuilder.Configuration[KeySetting]!];

        siloBuilder.AddMemoryGrainStorage(TripsModule.StorageName);
        siloBuilder.Services.AddSingleton<TimeProvider>(clock);
        siloBuilder.Services.AddSingleton<TripsMetrics>();
    }
}
