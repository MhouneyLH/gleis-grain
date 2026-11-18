using GleisGrain.Common.Errors;
using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

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
