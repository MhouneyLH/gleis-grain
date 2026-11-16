using System.Reflection;
using GleisGrain.Alerts;
using GleisGrain.Alerts.Contracts;
using GleisGrain.Cluster;
using GleisGrain.Cluster.Contracts;
using GleisGrain.Game;
using GleisGrain.Game.Contracts;
using GleisGrain.Ingestion;
using GleisGrain.Ingestion.Contracts;
using GleisGrain.Stops;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Modules.Tests;

/// <summary>
/// Keeps the modular monolith modular: modules talk to each other only through Contracts,
/// and expose nothing but their Module class.
/// </summary>
public class ModuleBoundaryTests
{
    private static readonly (string Name, Assembly Module, Assembly Contracts, Type ModuleType)[] _modules =
    [
        ("Stops", typeof(StopsModule).Assembly, typeof(StopId).Assembly, typeof(StopsModule)),
        ("Trips", typeof(TripsModule).Assembly, typeof(TripId).Assembly, typeof(TripsModule)),
        ("Alerts", typeof(AlertsModule).Assembly, typeof(AlertId).Assembly, typeof(AlertsModule)),
        ("Ingestion", typeof(IngestionModule).Assembly, typeof(SourceId).Assembly, typeof(IngestionModule)),
        ("Game", typeof(GameModule).Assembly, typeof(Nickname).Assembly, typeof(GameModule)),
        ("Cluster", typeof(ClusterModule).Assembly, typeof(LiveEvent).Assembly, typeof(ClusterModule)),
    ];

    public static TheoryData<string> ModuleNames => [.. _modules.Select(m => m.Name)];

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_DependsOnOtherModulesOnlyThroughTheirContracts(string name)
    {
        var module = _modules.Single(m => m.Name == name);
        var forbidden = _modules
            .Where(other => other.Name != name)
            .Select(other => other.Module.GetName().Name)
            .ToHashSet();

        var references = module.Module.GetReferencedAssemblies().Select(a => a.Name).ToList();

        Assert.DoesNotContain(references, reference => forbidden.Contains(reference));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_DoesNotDependOnTheHost(string name)
    {
        var module = _modules.Single(m => m.Name == name);

        var references = module.Module.GetReferencedAssemblies().Select(a => a.Name);

        Assert.DoesNotContain("GleisGrain.Host", references);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_ExposesOnlyItsModuleClass(string name)
    {
        var module = _modules.Single(m => m.Name == name);

        var exported = module.Module.GetExportedTypes();

        Assert.Equal([module.ModuleType], exported);
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Contracts_DoNotDependOnModulesOrAspNetCore(string name)
    {
        var module = _modules.Single(m => m.Name == name);

        var references = module.Contracts.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        Assert.DoesNotContain(references, reference => reference.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, reference => reference == "GleisGrain.Common");
        Assert.DoesNotContain(references, reference => _modules.Any(m => m.Module.GetName().Name == reference));
    }
}
