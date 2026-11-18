using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace GleisGrain.Simulator;

/// <summary>The control API of the presenter. JSON is camelCase, errors are RFC 9457 problem details.</summary>
public static class ScenarioEndpoints
{
    public static IEndpointRouteBuilder MapScenario(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/scenario");

        group.MapGet("", (AudienceSimulator simulator) => TypedResults.Ok(simulator.Snapshot()));

        group.MapPut("/pinned/{stopId}", (string stopId, PinRequest? request, AudienceSimulator simulator) =>
            request?.People is not { } people
                ? Problem("Body must be {\"people\": <number>}.", StatusCodes.Status400BadRequest)
                : Run(() => simulator.Pin(stopId, people)));

        group.MapDelete("/pinned", (AudienceSimulator simulator) => Run(simulator.ReleaseAll));

        return routes;
    }

    private static IResult Run(Func<ScenarioSnapshot> action)
    {
        try
        {
            return TypedResults.Ok(action());
        }
        catch (ScenarioException ex)
        {
            return Problem(ex.Message, StatusCodes.Status400BadRequest);
        }
        catch (ScenarioNotReadyException ex)
        {
            return Problem(ex.Message, StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static ProblemHttpResult Problem(string detail, int status) =>
        TypedResults.Problem(detail: detail, statusCode: status, title: status == StatusCodes.Status400BadRequest ? "Invalid scenario request" : "Simulator not ready");

    private sealed record PinRequest(int? People);
}
