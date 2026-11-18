using System.Text.Json;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops.Tests;

public class GrainVersionTests
{
    private static StopBoardSnapshot Snapshot() =>
        new(StopId.From("8f3a1c"), "Hauptbahnhof", 48.29, 14.29, 0, null, []);

    [Fact]
    public void Module_GrainVersion_IsTheContractsBuildVersion() =>
        Assert.Equal(StopsGrainVersion.Current, StopsModule.GrainVersion);

    [Fact]
    public void Module_GrainVersion_IsOneOrTwo() =>
        Assert.Contains(StopsModule.GrainVersion, new[] { 1, 2 });

    [Fact]
    public void Snapshot_ByDefault_CarriesTheBuildVersion() =>
        Assert.Equal(StopsModule.GrainVersion, Snapshot().GrainVersion);

    [Fact]
    public void Snapshot_Json_ExposesGrainVersion() =>
        Assert.Contains($"\"GrainVersion\":{StopsModule.GrainVersion}", JsonSerializer.Serialize(Snapshot()), StringComparison.Ordinal);
}
