using System.Collections.Concurrent;
using GleisGrain.Common.Options;
using GleisGrain.Stops.Catalogue;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace GleisGrain.Stops.Tests.Fakes;

/// <summary>
/// Configures the silos of a test cluster. The cluster builds its silos from the type name, so the fakes
/// reach them through a static registry; the test passes the registry key as a configuration setting.
/// </summary>
public sealed class StopsSiloConfigurator : ISiloConfigurator
{
    public const string KeySetting = "Tests:FakesKey";

    public static ConcurrentDictionary<string, StopsFakes> Registry { get; } = new();

    public void Configure(ISiloBuilder siloBuilder)
    {
        var fakes = Registry[siloBuilder.Configuration[KeySetting]!];

        siloBuilder.AddMemoryGrainStorage(StopsModule.StorageName);
        siloBuilder.UseInMemoryReminderService();

        siloBuilder.Services.AddValidatedOptions<StopsOptions>(StopsOptions.SectionName);
        siloBuilder.Services.AddSingleton(fakes);
        siloBuilder.Services.AddSingleton<TimeProvider>(new TestClock(fakes.Time));
        siloBuilder.Services.AddSingleton(StopCatalogue.Load());
        siloBuilder.Services.AddSingleton<StopsMetrics>();
    }
}
