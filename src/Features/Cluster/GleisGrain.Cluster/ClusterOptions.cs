using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Cluster;

internal sealed class ClusterOptions
{
    public const string SectionName = "Cluster";

    /// <summary>The chaos button kills a silo. Off by default so nobody can do that by accident.</summary>
    public bool EnableChaos { get; init; }

    /// <summary>
    /// How long the hub keeps a silo subscribed without hearing from it again. Every silo renews its subscription
    /// every third of this time, so a silo that died stops being notified after at most this long.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:03", "01:00:00")]
    public TimeSpan SubscriptionTtl { get; init; } = TimeSpan.FromMinutes(1);
}
