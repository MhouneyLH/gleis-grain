using System.ComponentModel;
using GleisGrain.Common.Errors;
using GleisGrain.Trips.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GleisGrain.Trips.Endpoints;

internal sealed record GetTripRequest
{
    [FromRoute(Name = "id")]
    [Description("Identifier of the trip, as shown on the departure board.")]
    public required TripId Id { get; init; }
}

internal sealed record GetTripResponse
{
    [Description("Identifier of the trip.")]
    public required TripId Id { get; init; }

    [Description("The stop that reported the trip.")]
    public required string StopId { get; init; }

    [Description("Line name.")]
    public required string Line { get; init; }

    [Description("Direction of the line.")]
    public required string Direction { get; init; }

    [Description("Timetable departure time.")]
    public required DateTimeOffset PlannedAt { get; init; }

    [Description("Latest delay in minutes. Final once the trip has departed.")]
    public required int DelayMinutes { get; init; }

    [Description("True once planned time plus delay has passed.")]
    public required bool HasDeparted { get; init; }
}

internal sealed class GetTrip
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/{id}", Handle)
            .WithName("GetTrip")
            .WithSummary("Gets a trip by id.")
            .WithDescription("Part of the Trips context: the delay facts bets are resolved against.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Ok<GetTripResponse>> Handle(
        [AsParameters] GetTripRequest request,
        IGrainFactory grains,
        TripsMetrics metrics,
        ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger<GetTrip>().LookingUpTrip(request.Id);

        var snapshot = await grains.GetGrain<ITripGrain>(request.Id.Value).GetAsync();
        metrics.TripLookedUp(snapshot is not null);

        if (snapshot is null)
        {
            throw new DomainException(new TripNotFound(request.Id));
        }

        return TypedResults.Ok(new GetTripResponse
        {
            Id = snapshot.Id,
            StopId = snapshot.StopId,
            Line = snapshot.Line,
            Direction = snapshot.Direction,
            PlannedAt = snapshot.PlannedAt,
            DelayMinutes = snapshot.DelayMinutes,
            HasDeparted = snapshot.HasDeparted,
        });
    }
}
