namespace GleisGrain.Cluster;

internal sealed class ClusterOptions
{
    public const string SectionName = "Cluster";

    /// <summary>The chaos button kills a silo. Off by default so nobody can do that by accident.</summary>
    public bool EnableChaos { get; init; }
}
