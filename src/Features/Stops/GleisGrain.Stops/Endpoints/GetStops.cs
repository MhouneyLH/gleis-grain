using System.ComponentModel;
using GleisGrain.Stops.Catalogue;
using GleisGrain.Stops.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GleisGrain.Stops.Endpoints;

internal sealed record StopListItem
{
    [Description("Identifier of the stop.")]
    public required StopId Id { get; init; }

    [Description("Name of the stop.")]
    public required string Name { get; init; }

    [Description("Latitude in degrees.")]
    public required double Latitude { get; init; }

    [Description("Longitude in degrees.")]
    public required double Longitude { get; init; }
}

internal sealed class GetStops
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet(string.Empty, Handle)
            .WithName("GetStops")
            .WithSummary("Lists the curated stops.")
            .WithDescription("Static data from the catalogue. Does not activate any stop grain.");

    private static Ok<List<StopListItem>> Handle(StopCatalogue catalogue) =>
        TypedResults.Ok(catalogue.All
            .Select(stop => new StopListItem { Id = stop.Id, Name = stop.Name, Latitude = stop.Latitude, Longitude = stop.Longitude })
            .ToList());
}
