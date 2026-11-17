namespace GleisGrain.Stops.Domain;

/// <summary>Persisted blob. Only add fields with new ids; bump <see cref="CurrentSchemaVersion"/> and migrate on activation.</summary>
[GenerateSerializer, Alias("GleisGrain.Stops.Domain.StopState")]
internal sealed class StopState
{
    public const int CurrentSchemaVersion = 1;

    [Id(0)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [Id(1)]
    public List<string> Watchers { get; set; } = [];

    [Id(2)]
    public List<DepartureState> Board { get; set; } = [];

    [Id(3)]
    public DateTimeOffset? RefreshedAt { get; set; }

    public static StopState Of(Stop stop) => new()
    {
        Watchers = [.. stop.Watchers],
        Board = [.. stop.Board.Select(DepartureState.Of)],
        RefreshedAt = stop.RefreshedAt,
    };

    public Stop ToStop(StopInfo info) =>
        new(info, Watchers, Board.Select(d => d.ToDeparture()), RefreshedAt);
}

[GenerateSerializer, Alias("GleisGrain.Stops.Domain.DepartureState")]
internal sealed class DepartureState
{
    [Id(0)]
    public string TripId { get; set; } = string.Empty;

    [Id(1)]
    public string Line { get; set; } = string.Empty;

    [Id(2)]
    public string Direction { get; set; } = string.Empty;

    [Id(3)]
    public DateTimeOffset PlannedAt { get; set; }

    [Id(4)]
    public int DelayMinutes { get; set; }

    [Id(5)]
    public string? Platform { get; set; }

    public static DepartureState Of(Departure departure) => new()
    {
        TripId = departure.TripId.Value,
        Line = departure.Line,
        Direction = departure.Direction,
        PlannedAt = departure.PlannedAt,
        DelayMinutes = departure.DelayMinutes,
        Platform = departure.Platform,
    };

    public Departure ToDeparture() =>
        new(GleisGrain.Trips.Contracts.TripId.From(TripId), Line, Direction, PlannedAt, DelayMinutes, Platform);
}
