using GleisGrain.Common.Errors;
using GleisGrain.Trips.Contracts;

namespace GleisGrain.Trips;

internal sealed record TripNotFound(TripId TripId) : DomainError(
    "trips.trip.not_found",
    "Trip not found",
    StatusCodes.Status404NotFound,
    $"Trip '{TripId}' is unknown. Trips appear once a stop has reported them.");
