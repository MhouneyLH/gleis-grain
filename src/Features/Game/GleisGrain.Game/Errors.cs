using GleisGrain.Common.Errors;
using GleisGrain.Game.Contracts;
using GleisGrain.Game.Domain;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game;

internal sealed record NicknameTaken(Nickname Nickname) : DomainError(
    "game.nickname.taken",
    "Nickname taken",
    StatusCodes.Status409Conflict,
    $"Nickname '{Nickname}' is already taken.");

internal sealed record PlayerNotFound(Nickname Nickname) : DomainError(
    "game.player.not_found",
    "Player not found",
    StatusCodes.Status404NotFound,
    $"Player '{Nickname}' does not exist.");

internal sealed record StopNotFound(StopId StopId) : DomainError(
    "game.stop.not_found",
    "Stop not found",
    StatusCodes.Status404NotFound,
    $"Stop '{StopId}' does not exist.");

internal sealed record TripNotFound(TripId TripId) : DomainError(
    "game.trip.not_found",
    "Trip not found",
    StatusCodes.Status404NotFound,
    $"Trip '{TripId}' is unknown.");

internal sealed record BetAlreadyOpen(Nickname Nickname) : DomainError(
    "game.bet.already_open",
    "Bet already open",
    StatusCodes.Status409Conflict,
    $"Player '{Nickname}' already has an open bet.");

internal sealed record TripDeparted(TripId TripId) : DomainError(
    "game.trip.departed",
    "Trip already departed",
    StatusCodes.Status409Conflict,
    $"Trip '{TripId}' has already departed.");

internal sealed record InvalidGuess(int Minutes) : DomainError(
    "game.bet.invalid_guess",
    "Invalid guess",
    StatusCodes.Status400BadRequest,
    $"A guess is {Bet.MinGuess} to {Bet.MaxGuess} whole minutes, not {Minutes}.");
