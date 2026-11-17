using GleisGrain.Trips.Contracts;

namespace GleisGrain.Stops.Domain;

/// <summary>One line of the departure board. <see cref="PlannedAt"/> is the timetable time, the delay comes on top.</summary>
internal sealed record Departure(
    TripId TripId,
    string Line,
    string Direction,
    DateTimeOffset PlannedAt,
    int DelayMinutes,
    string? Platform);
