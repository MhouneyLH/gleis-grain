namespace GleisGrain.Simulator;

public enum PersonAction
{
    None,
    Join,
    Move,
    ReadBoard,
}

/// <summary>
/// One person of the audience. It decides what it wants to do next and when, and knows nothing about HTTP:
/// whoever drives it asks <see cref="Due"/>, performs the action and reports the outcome.
/// </summary>
public sealed class VirtualPerson
{
    private static readonly TimeSpan MinMoveInterval = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan MaxMoveInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan MinReadInterval = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan MaxReadInterval = TimeSpan.FromSeconds(18);
    private static readonly TimeSpan MinRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(5);

    private readonly StopPicker _picker;
    private readonly Random _random;
    private DateTimeOffset _notBefore;
    private DateTimeOffset _nextMoveAt;
    private DateTimeOffset _nextReadAt;

    public VirtualPerson(string nickname, DateTimeOffset joinsAt, StopPicker picker, Random random)
    {
        Nickname = nickname;
        _notBefore = joinsAt;
        _picker = picker;
        _random = random;
    }

    public string Nickname { get; }

    /// <summary>The stop the person stands at. Null until the person has joined.</summary>
    public string? StopId { get; private set; }

    public PersonAction Due(DateTimeOffset now)
    {
        if (now < _notBefore)
        {
            return PersonAction.None;
        }

        if (StopId is null)
        {
            return PersonAction.Join;
        }

        if (now >= _nextMoveAt)
        {
            return PersonAction.Move;
        }

        return now >= _nextReadAt ? PersonAction.ReadBoard : PersonAction.None;
    }

    /// <summary>The stop the person goes to on <see cref="PersonAction.Join"/> or <see cref="PersonAction.Move"/>.</summary>
    public string ChooseStop() => StopId is null ? _picker.Pick() : _picker.PickOther(StopId);

    public void Joined(string stopId, DateTimeOffset now)
    {
        StopId = stopId;
        _nextMoveAt = now + Between(MinMoveInterval, MaxMoveInterval);
        _nextReadAt = now + Between(MinReadInterval, MaxReadInterval);
    }

    public void Moved(string stopId, DateTimeOffset now)
    {
        StopId = stopId;
        _nextMoveAt = now + Between(MinMoveInterval, MaxMoveInterval);
        _nextReadAt = now + Between(MinReadInterval, MaxReadInterval);
    }

    public void BoardRead(DateTimeOffset now) => _nextReadAt = now + Between(MinReadInterval, MaxReadInterval);

    /// <summary>The action did not work (host down, silo killed). Try again a little later.</summary>
    public void Failed(DateTimeOffset now) => _notBefore = now + Between(MinRetryDelay, MaxRetryDelay);

    private TimeSpan Between(TimeSpan min, TimeSpan max) => min + ((max - min) * _random.NextDouble());
}
