using GleisGrain.Common.Errors;

namespace GleisGrain.Alerts;

/// <summary>The notices were never loaded and the source cannot be reached right now.</summary>
internal sealed record AlertsUnavailable() : DomainError(
    "alerts.source.unavailable",
    "Service notices unavailable",
    StatusCodes.Status503ServiceUnavailable,
    "The service notices cannot be loaded right now. Try again later.");
