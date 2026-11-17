using GleisGrain.Stops.Contracts;

namespace GleisGrain.Stops.Domain;

/// <summary>
/// A stop with its watchers and its departure board. Pure domain model: no Orleans runtime, no I/O.
/// Time comes in as a parameter.
/// </summary>
internal sealed class Stop
{
    private readonly HashSet<string> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private List<Departure> _board = [];

    public Stop(
        StopInfo info,
        IEnumerable<string>? watchers = null,
        IEnumerable<Departure>? board = null,
        DateTimeOffset? refreshedAt = null)
    {
        Info = info;
        _watchers.UnionWith(watchers ?? []);
        _board = [.. board ?? []];
        RefreshedAt = refreshedAt;
    }

    public StopInfo Info { get; }

    public IReadOnlyCollection<string> Watchers => _watchers;

    public IReadOnlyList<Departure> Board => _board;

    public DateTimeOffset? RefreshedAt { get; private set; }

    /// <summary>Adds a watcher. Returns false when the watcher was already there.</summary>
    public bool Watch(string watcher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(watcher);

        return _watchers.Add(watcher);
    }

    /// <summary>Removes a watcher. Returns false when the watcher was not there.</summary>
    public bool Unwatch(string watcher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(watcher);

        return _watchers.Remove(watcher);
    }

    public bool NeedsRefresh(DateTimeOffset now, TimeSpan maxAge) =>
        RefreshedAt is not { } refreshedAt || now - refreshedAt >= maxAge;

    /// <summary>Replaces the board with the latest departures, earliest first.</summary>
    public void UpdateBoard(IEnumerable<Departure> departures, DateTimeOffset now)
    {
        _board = [.. departures.OrderBy(departure => departure.PlannedAt).ThenBy(departure => departure.Line, StringComparer.Ordinal)];
        RefreshedAt = now;
    }

    public StopBoardSnapshot ToSnapshot() =>
        new(
            Info.Id,
            Info.Name,
            Info.Latitude,
            Info.Longitude,
            _watchers.Count,
            RefreshedAt,
            [.. _board.Select(d => new BoardDeparture(d.TripId, d.Line, d.Direction, d.PlannedAt, d.DelayMinutes, d.Platform))]);
}
