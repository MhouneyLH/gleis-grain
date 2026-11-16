using System.ComponentModel;
using GleisGrain.Common.Errors;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GleisGrain.Game.Endpoints;

internal sealed record WatchStopBody
{
    [Description("Identifier of the stop to watch.")]
    public required StopId StopId { get; init; }
}

internal sealed record WatchStopRequest
{
    [FromRoute(Name = "nickname")]
    [Description("The player's nickname, case does not matter.")]
    public required Nickname Nickname { get; init; }

    [FromBody]
    public required WatchStopBody Body { get; init; }
}

internal sealed class WatchStop
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapPut("/players/{nickname}/stop", Handle)
            .WithName("WatchStop")
            .WithSummary("Moves the player to a stop.")
            .WithDescription("Part of the Game context: the player leaves the old stop and becomes a watcher of the new one.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<NoContent> Handle([AsParameters] WatchStopRequest request, IGrainFactory grains)
    {
        var outcome = await grains.GetGrain<IPlayerGrain>(request.Nickname.Key).WatchStopAsync(request.Body.StopId);

        return outcome switch
        {
            WatchStopOutcome.Watching => TypedResults.NoContent(),
            WatchStopOutcome.NoPlayer => throw new DomainException(new PlayerNotFound(request.Nickname)),
            WatchStopOutcome.UnknownStop => throw new DomainException(new StopNotFound(request.Body.StopId)),
            _ => throw new InvalidOperationException($"Unexpected outcome {outcome}."),
        };
    }
}
