using GleisGrain.Common.Errors;

namespace GleisGrain.Cluster;

internal sealed record ChaosDisabled() : DomainError(
    "cluster.chaos.disabled",
    "Chaos is disabled",
    StatusCodes.Status404NotFound,
    "The chaos button is switched off. Set 'Cluster:EnableChaos' to true to use it.");
