using System.ComponentModel;
using System.Xml;
using GleisGrain.Alerts.Contracts;
using GleisGrain.Common.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GleisGrain.Alerts.Endpoints;

internal sealed record GetAlertsRequest
{
    [FromQuery(Name = "line")]
    [Description("Only notices naming this line, for example 26 (L 26 works too).")]
    public string? Line { get; init; }

    [FromQuery(Name = "stop")]
    [Description("Only notices naming this stop id.")]
    public string? Stop { get; init; }
}

/// <summary>One service notice.</summary>
internal sealed record GetAlertsResponse
{
    [Description("Identifier of the notice.")]
    public required AlertId Id { get; init; }

    [Description("Short title, usually starting with the lines it concerns.")]
    public required string Title { get; init; }

    [Description("The notice as plain text.")]
    public required string Text { get; init; }

    [Description("Lines the notice names. Empty when it names none.")]
    public required IReadOnlyList<string> Lines { get; init; }

    [Description("Ids of the stops the notice names. Empty when it names none.")]
    public required IReadOnlyList<string> StopIds { get; init; }

    [Description("When this service saw the notice for the first time.")]
    public required DateTimeOffset FirstSeenAt { get; init; }
}

internal static class GetAlerts
{
    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet(string.Empty, Handle)
            .WithName("GetAlerts")
            .WithSummary("Lists the current service notices.")
            .WithDescription("Part of the Alerts context: notices from Linz AG, optionally for one line or one stop.")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    private static async Task<Ok<IReadOnlyList<GetAlertsResponse>>> Handle(
        [AsParameters] GetAlertsRequest request,
        IGrainFactory grains)
    {
        IReadOnlyList<AlertSnapshot> alerts;
        try
        {
            alerts = await grains.GetGrain<IAlertBoardGrain>(AlertBoardKeys.Linz).GetAsync(request.Line, request.Stop);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidDataException or XmlException)
        {
            throw new DomainException(new AlertsUnavailable());
        }

        return TypedResults.Ok<IReadOnlyList<GetAlertsResponse>>(
            [.. alerts.Select(alert => new GetAlertsResponse
            {
                Id = alert.Id,
                Title = alert.Title,
                Text = alert.Text,
                Lines = alert.Lines,
                StopIds = alert.StopIds,
                FirstSeenAt = alert.FirstSeenAt,
            })]);
    }
}
