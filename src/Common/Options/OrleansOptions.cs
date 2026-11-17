using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Common.Options;

public sealed class OrleansOptions
{
    public const string SectionName = "Orleans";

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ClusterId { get; init; } = "gleis-grain";

    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string ServiceId { get; init; } = "gleis-grain";

    /// <summary>Name of the connection string holding the cluster membership and reminder tables.</summary>
    [Required]
    public string ClusteringConnectionName { get; init; } = "orleans";

    /// <summary>
    /// Upper bound of pooled Postgres connections per connection string and process. A cluster has one pool per
    /// database and replica, so without a cap a burst of grain activations opens more connections than the server allows.
    /// </summary>
    [Range(1, 500)]
    public int MaxConnectionPoolSize { get; init; } = 20;

    [Range(1, 65_535)]
    public int SiloPort { get; init; } = 11_111;

    [Range(1, 65_535)]
    public int GatewayPort { get; init; } = 30_000;

    /// <summary>Exposes the Orleans Dashboard. Off by default because it shows internals of the cluster.</summary>
    public bool EnableDashboard { get; init; }

    [RegularExpression("^/.+")]
    public string DashboardPath { get; init; } = "/dashboard";
}
