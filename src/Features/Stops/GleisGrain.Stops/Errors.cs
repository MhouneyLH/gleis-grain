using GleisGrain.Common.Errors;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops;

internal sealed record StopNotFound(StopId StopId) : DomainError(
    "stops.stop.not_found",
    "Stop not found",
    StatusCodes.Status404NotFound,
    $"Stop '{StopId}' is not part of the curated stops.");
