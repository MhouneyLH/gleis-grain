using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GleisGrain.Cluster.Endpoints;

internal sealed record ActivationResponse
{
    [Description("Identifier of the stop whose grain is active.")]
    public required string StopId { get; init; }

    [Description("Name of the silo that hosts the grain, as listed by the silos endpoint.")]
    public required string Silo { get; init; }
}

internal sealed class GetActivations
{
    /// <summary>
    /// The grain type name of the stop grain. The Stops module declares its grain class with
    /// <c>[GrainType("stop")]</c>; this is the only place in Cluster that knows the name.
    /// </summary>
    public const string StopGrainType = "stop";

    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/activations", Handle)
            .WithName("GetActivations")
            .WithSummary("Lists the stop grains that are active right now.")
            .WithDescription("Part of the Cluster context: lets the map start in the right state.");

    private static async Task<Ok<IReadOnlyList<ActivationResponse>>> Handle(IGrainFactory grains)
    {
        var management = grains.GetGrain<IManagementGrain>(0);
        var statistics = await management.GetDetailedGrainStatistics();
        var hosts = await management.GetDetailedHosts();
        var names = hosts
            .GroupBy(host => host.SiloAddress)
            .ToDictionary(group => group.Key, group => group.First().SiloName);

        IReadOnlyList<ActivationResponse> activations =
        [
            .. statistics
                // The statistics name the grain class, not the grain type, so the type is read from the grain id.
                .Where(statistic => statistic.GrainId.Type.ToString() == StopGrainType)
                .Select(statistic => new ActivationResponse
                {
                    StopId = statistic.GrainId.Key.ToString()!,
                    Silo = names.GetValueOrDefault(statistic.SiloAddress, statistic.SiloAddress.ToString()),
                })
                .OrderBy(activation => activation.StopId, StringComparer.Ordinal),
        ];

        return TypedResults.Ok(activations);
    }
}
