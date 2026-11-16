using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GleisGrain.Cluster.Endpoints;

internal sealed record SiloResponse
{
    [Description("Name of the silo.")]
    public required string Name { get; init; }

    [Description("Silo address, including the generation, so a restarted silo is a new entry.")]
    public required string Address { get; init; }

    [Description("Membership status as text, for example Active, ShuttingDown or Dead.")]
    public required string Status { get; init; }
}

internal sealed class GetSilos
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/silos", Handle)
            .WithName("GetSilos")
            .WithSummary("Lists the silos of the cluster.")
            .WithDescription("Part of the Cluster context: read from Orleans membership. Dead silos are included with their status.");

    private static async Task<Ok<IReadOnlyList<SiloResponse>>> Handle(IGrainFactory grains)
    {
        var hosts = await grains.GetGrain<IManagementGrain>(0).GetDetailedHosts();

        IReadOnlyList<SiloResponse> silos =
        [
            .. hosts
                .OrderBy(host => host.SiloName, StringComparer.Ordinal)
                .ThenBy(host => host.SiloAddress.Generation)
                .Select(host => new SiloResponse
                {
                    Name = host.SiloName,
                    Address = host.SiloAddress.ToString(),
                    Status = host.Status.ToString(),
                }),
        ];

        return TypedResults.Ok(silos);
    }
}
