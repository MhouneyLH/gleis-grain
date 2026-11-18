namespace GleisGrain.Simulator;

/// <summary>Chooses stops like a crowd does: a share of the picks goes to one hot stop, the rest is spread evenly over the other stops.</summary>
public sealed class StopPicker
{
    /// <summary>Hauptbahnhof, where everybody wants to be.</summary>
    public const string HauptbahnhofId = "60501720";

    private readonly string? _hotStop;
    private readonly string[] _others;
    private readonly double _hotShare;
    private readonly Random _random;

    public StopPicker(IReadOnlyCollection<string> stopIds, string hotStopId, double hotShare, Random random)
    {
        ArgumentNullException.ThrowIfNull(stopIds);
        ArgumentNullException.ThrowIfNull(random);
        if (stopIds.Count == 0)
        {
            throw new ArgumentException("At least one stop is needed.", nameof(stopIds));
        }

        _hotStop = stopIds.Contains(hotStopId) ? hotStopId : null;
        _others = [.. stopIds.Where(id => id != _hotStop)];
        _hotShare = hotShare;
        _random = random;
    }

    public string Pick()
    {
        if (_others.Length == 0)
        {
            return _hotStop!;
        }

        return _hotStop is not null && _random.NextDouble() < _hotShare ? _hotStop : _others[_random.Next(_others.Length)];
    }

    /// <summary>Picks a stop that differs from <paramref name="current"/> (when there is more than one stop).</summary>
    public string PickOther(string current)
    {
        var pick = Pick();
        if (pick != current)
        {
            return pick;
        }

        var candidates = _others.Where(id => id != current).ToArray();
        return candidates.Length > 0 ? candidates[_random.Next(candidates.Length)] : _hotStop ?? current;
    }
}
