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

    [Range(1, 65_535)]
    public int SiloPort { get; init; } = 11_111;

    [Range(1, 65_535)]
    public int GatewayPort { get; init; } = 30_000;

    /// <summary>Exposes the Orleans Dashboard. Off by default because it shows internals of the cluster.</summary>
    public bool EnableDashboard { get; init; }

    [RegularExpression("^/.+")]
    public string DashboardPath { get; init; } = "/dashboard";
}
