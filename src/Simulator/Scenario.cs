namespace GleisGrain.Simulator;

/// <summary>A request the presenter made that cannot be fulfilled. The message is meant for the presenter.</summary>
public sealed class ScenarioException(string message) : Exception(message);

/// <summary>The stop catalogue is not loaded yet (the host is not reachable), so no stop can be validated.</summary>
public sealed class ScenarioNotReadyException() : InvalidOperationException("The simulator has not loaded the stop catalogue yet.");

public sealed record PinnedStop(string StopId, int People);

/// <summary>
/// The virtual people and what the presenter steers: how many of them are pinned to which stop. Pure logic, not thread safe:
/// whoever shares it between threads takes a lock.
/// </summary>
public sealed class Scenario
{
    private readonly List<VirtualPerson> _people;
    private readonly SimulatorOptions _options;
    private readonly StopPicker _picker;
    private readonly HashSet<string> _stopIds;
    private readonly Random _random;

    public Scenario(SimulatorOptions options, IReadOnlyCollection<string> stopIds, StopPicker picker, Random random, DateTimeOffset start)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(stopIds);
        _options = options;
        _picker = picker;
        _random = random;
        _stopIds = [.. stopIds];
        _people = [.. Crowd.Create(options, picker, random, start)];
    }

    public IReadOnlyList<VirtualPerson> People => _people;

    public int Joined => _people.Count(person => person.StopId is not null);

    public IReadOnlyList<PinnedStop> Pinned =>
    [
        .. _people
            .Where(person => person.PinnedStopId is not null)
            .GroupBy(person => person.PinnedStopId!)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PinnedStop(group.Key, group.Count())),
    ];

    /// <summary>Exactly <paramref name="people"/> virtual people are pinned to the stop. Zero removes the pin.</summary>
    public void Pin(string stopId, int people, DateTimeOffset now)
    {
        if (!_stopIds.Contains(stopId))
        {
            throw new ScenarioException($"Unknown stop '{stopId}'.");
        }

        if (people < 0 || people > _options.MaxPeople)
        {
            throw new ScenarioException($"people must be between 0 and {_options.MaxPeople}.");
        }

        var pinnedHere = _people.Where(person => person.PinnedStopId == stopId).ToList();
        var pinnedElsewhere = _people.Count(person => person.PinnedStopId is not null && person.PinnedStopId != stopId);
        if (pinnedElsewhere + people > _options.MaxPeople)
        {
            throw new ScenarioException($"At most {_options.MaxPeople} people can be pinned in total, {pinnedElsewhere} are pinned at other stops.");
        }

        foreach (var surplus in pinnedHere.Skip(people))
        {
            surplus.Release(now);
        }

        var missing = people - pinnedHere.Count;
        if (missing <= 0)
        {
            return;
        }

        var recruits = _people.Where(person => person.PinnedStopId is null).Take(missing).ToList();
        foreach (var person in recruits)
        {
            person.Pin(stopId, now);
        }

        // Not enough free people: new ones join at once, without ramp-up.
        for (var created = recruits.Count; created < missing; created++)
        {
            var person = new VirtualPerson(Nicknames.For(_people.Count + 1), now, _picker, _random);
            person.Pin(stopId, now);
            _people.Add(person);
        }
    }

    public void ReleaseAll(DateTimeOffset now)
    {
        foreach (var person in _people.Where(person => person.PinnedStopId is not null))
        {
            person.Release(now);
        }
    }
}

/// <summary>The body of <c>GET /scenario</c>.</summary>
public sealed record ScenarioSnapshot(int People, int Joined, IReadOnlyList<PinnedStop> Pinned, double HotStopShare);
