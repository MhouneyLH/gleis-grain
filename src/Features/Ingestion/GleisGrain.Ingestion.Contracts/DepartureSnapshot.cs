namespace GleisGrain.Ingestion.Contracts;

/// <summary>One departure as the EFA server reports it. <see cref="PlannedAt"/> is the timetable time, delay comes on top.</summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Ingestion.Contracts.DepartureSnapshot")]
public sealed record DepartureSnapshot(
    [property: Id(0)] string Line,
    [property: Id(1)] string Direction,
    [property: Id(2)] DateTimeOffset PlannedAt,
    [property: Id(3)] int DelayMinutes,
    [property: Id(4)] string? Platform);
