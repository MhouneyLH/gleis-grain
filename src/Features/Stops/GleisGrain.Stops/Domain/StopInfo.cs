using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops.Domain;

/// <summary>What the catalogue knows about a stop. The id is also the stop id of the EFA server.</summary>
internal sealed record StopInfo(StopId Id, string Name, double Latitude, double Longitude);
