using GleisGrain.Common.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace GleisGrain.Cluster.Endpoints;

internal sealed class KillSilo
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapPost("/chaos/kill", Handle)
            .WithName("KillSilo")
            .WithSummary("Kills the replica that receives the request.")
            .WithDescription("Part of the Cluster context: answers 202, then the process ends abruptly so membership has to detect the death.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static Accepted Handle(IOptions<ClusterOptions> options, IProcessKiller killer, ILoggerFactory loggerFactory)
    {
        if (!options.Value.EnableChaos)
        {
            throw new DomainException(new ChaosDisabled());
        }

        loggerFactory.CreateLogger<KillSilo>().ChaosKillTriggered();
        killer.Kill();

        return TypedResults.Accepted((string?)null);
    }
}
