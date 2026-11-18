using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GleisGrain.Simulator;

/// <summary>
/// Drives the crowd through the public API. Every <see cref="TickAsync"/> performs what is due. Errors never escape:
/// the presenter kills silos on purpose, so a failed call is counted, logged and retried later.
/// </summary>
public sealed class AudienceSimulator(
    GleisGrainClient client,
    IOptions<SimulatorOptions> options,
    TimeProvider time,
    Random random,
    ILogger<AudienceSimulator> logger)
{
    private static readonly TimeSpan CatalogueRetryDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan SummaryInterval = TimeSpan.FromSeconds(10);

    private readonly SimulatorOptions _options = options.Value;
    // The worker thread ticks, request threads steer: every access to the scenario and its people takes the lock.
    private readonly Lock _gate = new();
    private Scenario? _scenario;
    private DateTimeOffset _catalogueRetryAt;
    private DateTimeOffset _nextSummaryAt;
    private int _moves;
    private int _reads;
    private int _errors;

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        if (_scenario is null)
        {
            await TryStartAsync(now, cancellationToken);
            return;
        }

        // Decisions (and the shared Random) stay under the lock; only the HTTP calls run in parallel.
        List<(VirtualPerson Person, PersonAction Action, string Stop)> work;
        lock (_gate)
        {
            work = _scenario.People
                .Select(person => (Person: person, Action: person.Due(now)))
                .Where(item => item.Action != PersonAction.None)
                .Select(item => (item.Person, item.Action, Stop: item.Action is PersonAction.ReadBoard ? item.Person.StopId! : item.Person.ChooseStop()))
                .ToList();
        }

        var succeeded = await Task.WhenAll(work.Select(item => PerformAsync(item.Person, item.Action, item.Stop, cancellationToken)));

        var finished = time.GetUtcNow();
        lock (_gate)
        {
            for (var i = 0; i < work.Count; i++)
            {
                Record(work[i].Person, work[i].Action, work[i].Stop, succeeded[i], finished);
            }

            if (finished >= _nextSummaryAt)
            {
                logger.Summary(_scenario.Joined, _moves, _reads, _errors);
                _nextSummaryAt = finished + SummaryInterval;
            }
        }
    }

    /// <summary>What the presenter has set up and how far the crowd has come.</summary>
    public ScenarioSnapshot Snapshot()
    {
        lock (_gate)
        {
            return SnapshotLocked();
        }
    }

    /// <summary>Exactly <paramref name="people"/> virtual people are pinned to the stop (see <see cref="Scenario.Pin"/>).</summary>
    /// <exception cref="ScenarioException">The request is invalid.</exception>
    /// <exception cref="ScenarioNotReadyException">The stop catalogue has not been loaded yet.</exception>
    public ScenarioSnapshot Pin(string stopId, int people)
    {
        lock (_gate)
        {
            Ready().Pin(stopId, people, time.GetUtcNow());
            return SnapshotLocked();
        }
    }

    /// <exception cref="ScenarioNotReadyException">The stop catalogue has not been loaded yet.</exception>
    public ScenarioSnapshot ReleaseAll()
    {
        lock (_gate)
        {
            Ready().ReleaseAll(time.GetUtcNow());
            return SnapshotLocked();
        }
    }

    private Scenario Ready() => _scenario ?? throw new ScenarioNotReadyException();

    private ScenarioSnapshot SnapshotLocked() =>
        _scenario is null
            ? new ScenarioSnapshot(0, 0, [], _options.HotStopShare)
            : new ScenarioSnapshot(_scenario.People.Count, _scenario.Joined, _scenario.Pinned, _options.HotStopShare);

    private async Task TryStartAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (now < _catalogueRetryAt)
        {
            return;
        }

        try
        {
            var stopIds = await client.GetStopIdsAsync(cancellationToken);
            var picker = new StopPicker(stopIds, StopPicker.HauptbahnhofId, _options.HotStopShare, random);
            lock (_gate)
            {
                _scenario = new Scenario(_options, stopIds, picker, random, now);
                _nextSummaryAt = now + SummaryInterval;
            }

            logger.Started(_options.People, stopIds.Count, _options.BaseUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or ArgumentException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.CatalogueUnavailable(ex.Message);
            _catalogueRetryAt = now + CatalogueRetryDelay;
        }
    }

    private async Task<bool> PerformAsync(VirtualPerson person, PersonAction action, string stopId, CancellationToken cancellationToken)
    {
        try
        {
            switch (action)
            {
                case PersonAction.Join:
                    await client.ClaimNicknameAsync(person.Nickname, cancellationToken);
                    await client.MoveToStopAsync(person.Nickname, stopId, cancellationToken);
                    break;
                case PersonAction.Move:
                    await client.MoveToStopAsync(person.Nickname, stopId, cancellationToken);
                    break;
                case PersonAction.ReadBoard:
                    await client.ReadBoardAsync(stopId, cancellationToken);
                    break;
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.ActionFailed(action, ex.Message);
            return false;
        }
    }

    private void Record(VirtualPerson person, PersonAction action, string stopId, bool succeeded, DateTimeOffset now)
    {
        if (!succeeded)
        {
            _errors++;
            person.Failed(now);
            return;
        }

        switch (action)
        {
            case PersonAction.Join:
                person.Joined(stopId, now);
                break;
            case PersonAction.Move:
                _moves++;
                person.Moved(stopId, now);
                break;
            case PersonAction.ReadBoard:
                _reads++;
                person.BoardRead(now);
                break;
        }
    }
}
