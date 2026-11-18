using Microsoft.Extensions.Logging;

namespace GleisGrain.Simulator;

internal static partial class SimulatorLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Simulator ready: {People} people, {Stops} stops, {BaseUrl}")]
    public static partial void Started(this ILogger logger, int people, int stops, string baseUrl);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stop catalogue not available, trying again: {Reason}")]
    public static partial void CatalogueUnavailable(this ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Action} failed, trying again later: {Reason}")]
    public static partial void ActionFailed(this ILogger logger, PersonAction action, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "Simulator: {Joined} people joined, {Moves} moves, {Reads} board reads, {Errors} errors")]
    public static partial void Summary(this ILogger logger, int joined, int moves, int reads, int errors);
}
