using System.ComponentModel;
using GleisGrain.Common.Errors;
using GleisGrain.Game.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GleisGrain.Game.Endpoints;

internal sealed record ClaimPlayerBody
{
    [Description("The nickname to claim: 2 to 20 letters, digits, spaces, dashes or underscores.")]
    public required Nickname Nickname { get; init; }
}

internal sealed record ClaimPlayerRequest
{
    [FromBody]
    public required ClaimPlayerBody Body { get; init; }
}

internal sealed class ClaimPlayer
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapPost("/players", Handle)
            .WithName("ClaimPlayer")
            .WithSummary("Claims a nickname and creates the player.")
            .WithDescription("Part of the Game context: players, bets and the leaderboard. The first claim of a nickname wins, case does not matter.")
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<CreatedAtRoute<PlayerResponse>> Handle([AsParameters] ClaimPlayerRequest request, IGrainFactory grains)
    {
        var nickname = request.Body.Nickname;
        var player = grains.GetGrain<IPlayerGrain>(nickname.Key);

        if (!await player.ClaimAsync(nickname))
        {
            throw new DomainException(new NicknameTaken(nickname));
        }

        var snapshot = await player.GetAsync() ?? throw new InvalidOperationException("A claimed player must exist.");

        return TypedResults.CreatedAtRoute(PlayerResponse.From(snapshot), GetPlayer.RouteName, new { nickname = nickname.Value });
    }
}
