using System.ComponentModel;
using GleisGrain.Game.Contracts;
using GleisGrain.Game.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GleisGrain.Game.Endpoints;

internal sealed record LeaderboardRowResponse
{
    [Description("The player's nickname.")]
    public required string Nickname { get; init; }

    [Description("Points earned so far.")]
    public required int Score { get; init; }
}

internal sealed class GetLeaderboard
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/leaderboard", Handle)
            .WithName("GetLeaderboard")
            .WithSummary("Gets the ten best players.")
            .WithDescription("Part of the Game context: highest score first, ties by nickname.");

    private static async Task<Ok<LeaderboardRowResponse[]>> Handle(IGrainFactory grains)
    {
        var top = await grains.GetGrain<ILeaderboardGrain>(LeaderboardKeys.Main).GetTopAsync(Leaderboard.TopSize);

        return TypedResults.Ok(top.Select(entry => new LeaderboardRowResponse { Nickname = entry.Nickname, Score = entry.Score }).ToArray());
    }
}
