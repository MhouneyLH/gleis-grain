using System.ComponentModel;
using GleisGrain.Common.Errors;
using GleisGrain.Stops.Catalogue;
using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Domain;
using GleisGrain.Trips.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GleisGrain.Stops.Endpoints;

internal sealed record GetStopRequest
{
    [FromRoute(Name = "id")]
    [Description("Identifier of the stop.")]
    public required StopId Id { get; init; }
}

internal sealed record GetStopResponse
{
    [Description("Identifier of the stop.")]
    public required StopId Id { get; init; }

    [Description("Name of the stop.")]
    public required string Name { get; init; }

    [Description("Latitude in degrees.")]
    public required double Latitude { get; init; }

    [Description("Longitude in degrees.")]
    public required double Longitude { get; init; }

    [Description("Number of people watching the stop.")]
    public required int Watchers { get; init; }

    [Description("When the board was last refreshed, null when it never was.")]
    public required DateTimeOffset? RefreshedAt { get; init; }

    [Description("Grain interface version (1 or 2) of the build that serves the stop grain. Changes when the grain moves to a silo of the other version.")]
    public required int GrainVersion { get; init; }

    [Description("The next departures, earliest first.")]
    public required IReadOnlyList<GetStopDeparture> Departures { get; init; }
}

internal sealed record GetStopDeparture
{
    [Description("Identifier of the trip, the key of its delay facts.")]
    public required TripId TripId { get; init; }

    [Description("Line name.")]
    public required string Line { get; init; }

    [Description("Direction of the line.")]
    public required string Direction { get; init; }

    [Description("Timetable departure time.")]
    public required DateTimeOffset PlannedAt { get; init; }

    [Description("Delay in minutes.")]
    public required int DelayMinutes { get; init; }

    [Description("Platform, null when unknown.")]
    public required string? Platform { get; init; }
}

internal sealed class GetStop
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/{id}", Handle)
            .WithName("GetStop")
            .WithSummary("Gets the departure board of a stop.")
            .WithDescription("Activates the stop grain. Part of the Stops context: stops and their departure boards.")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<Ok<GetStopResponse>> Handle(
        [AsParameters] GetStopRequest request,
        StopCatalogue catalogue,
        IGrainFactory grains,
        IOptions<StopsOptions> options,
        StopsMetrics metrics,
        ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger<GetStop>().LookingUpBoard(request.Id);

        // Ids outside the catalogue are turned away here, so arbitrary URLs cannot create grain activations.
        var board = catalogue.TryGet(request.Id, out _)
            ? await ReadBoardAsync(grains, request.Id, options.Value.UseBoardCache)
            : null;
        metrics.BoardLookedUp(board is not null);

        if (board is null)
        {
            throw new DomainException(new StopNotFound(request.Id));
        }

        return TypedResults.Ok(new GetStopResponse
        {
            Id = board.Id,
            Name = board.Name,
            Latitude = board.Latitude,
            Longitude = board.Longitude,
            Watchers = board.Watchers,
            RefreshedAt = board.RefreshedAt,
            GrainVersion = board.GrainVersion,
            Departures =
            [
                .. board.Departures.Select(d => new GetStopDeparture
                {
                    TripId = d.TripId,
                    Line = d.Line,
                    Direction = d.Direction,
                    PlannedAt = d.PlannedAt,
                    DelayMinutes = d.DelayMinutes,
                    Platform = d.Platform,
                }),
            ],
        });
    }

    private static Task<StopBoardSnapshot?> ReadBoardAsync(IGrainFactory grains, StopId id, bool useCache) =>
        useCache
            ? grains.GetGrain<IStopBoardCacheGrain>(id.Value).GetBoardAsync()
            : grains.GetGrain<IStopGrain>(id.Value).GetBoardAsync();
}
