using System.ComponentModel;
using GleisGrain.Common.Errors;
using GleisGrain.Game.Contracts;
using GleisGrain.Trips.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GleisGrain.Game.Endpoints;

internal sealed record PlaceBetBody
{
    [Description("Identifier of the trip to bet on.")]
    public required TripId TripId { get; init; }

    [Description("The guessed delay in whole minutes, 0 means on time.")]
    public required int GuessedDelayMinutes { get; init; }
}

internal sealed record PlaceBetRequest
{
    [FromRoute(Name = "nickname")]
    [Description("The player's nickname, case does not matter.")]
    public required Nickname Nickname { get; init; }

    [FromBody]
    public required PlaceBetBody Body { get; init; }
}

internal sealed class PlaceBet
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapPost("/players/{nickname}/bet", Handle)
            .WithName("PlaceBet")
            .WithSummary("Places the player's bet on the delay of a departure.")
            .WithDescription("Part of the Game context: a player has one open bet, which is resolved after the departure. 3 points for the exact delay, 1 point when one minute off.")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<CreatedAtRoute<PlayerResponse>> Handle([AsParameters] PlaceBetRequest request, IGrainFactory grains)
    {
        var player = grains.GetGrain<IPlayerGrain>(request.Nickname.Key);
        var tripId = request.Body.TripId;
        var guess = request.Body.GuessedDelayMinutes;

        var outcome = await player.PlaceBetAsync(tripId, guess);

        DomainError? error = outcome switch
        {
            PlaceBetOutcome.Placed => null,
            PlaceBetOutcome.NoPlayer => new PlayerNotFound(request.Nickname),
            PlaceBetOutcome.InvalidGuess => new InvalidGuess(guess),
            PlaceBetOutcome.UnknownTrip => new TripNotFound(tripId),
            PlaceBetOutcome.TripAlreadyDeparted => new TripDeparted(tripId),
            PlaceBetOutcome.AlreadyHasOpenBet => new BetAlreadyOpen(request.Nickname),
            _ => throw new InvalidOperationException($"Unexpected outcome {outcome}."),
        };

        if (error is not null)
        {
            throw new DomainException(error);
        }

        var snapshot = await player.GetAsync() ?? throw new InvalidOperationException("A player with a bet must exist.");

        return TypedResults.CreatedAtRoute(PlayerResponse.From(snapshot), GetPlayer.RouteName, new { nickname = request.Nickname.Value });
    }
}
