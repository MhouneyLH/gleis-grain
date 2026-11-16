using System.ComponentModel;
using GleisGrain.Common.Errors;
using GleisGrain.Game.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GleisGrain.Game.Endpoints;

internal sealed record GetPlayerRequest
{
    [FromRoute(Name = "nickname")]
    [Description("The player's nickname, case does not matter.")]
    public required Nickname Nickname { get; init; }
}

internal sealed class GetPlayer
{
    public const string RouteName = "GetPlayer";

    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/players/{nickname}", Handle)
            .WithName(RouteName)
            .WithSummary("Gets a player.")
            .WithDescription("Part of the Game context: players, bets and the leaderboard.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Ok<PlayerResponse>> Handle([AsParameters] GetPlayerRequest request, IGrainFactory grains)
    {
        var player = await grains.GetGrain<IPlayerGrain>(request.Nickname.Key).GetAsync()
            ?? throw new DomainException(new PlayerNotFound(request.Nickname));

        return TypedResults.Ok(PlayerResponse.From(player));
    }
}
