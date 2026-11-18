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
    private IReadOnlyList<VirtualPerson>? _crowd;
    private DateTimeOffset _catalogueRetryAt;
    private DateTimeOffset _nextSummaryAt;
    private int _moves;
    private int _reads;
    private int _errors;

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        if (_crowd is null)
        {
            await TryStartAsync(now, cancellationToken);
            return;
        }

        // Decisions (and the shared Random) stay on this thread; only the HTTP calls run in parallel.
        var work = _crowd
            .Select(person => (Person: person, Action: person.Due(now)))
            .Where(item => item.Action != PersonAction.None)
            .Select(item => (item.Person, item.Action, Stop: item.Action is PersonAction.ReadBoard ? item.Person.StopId! : item.Person.ChooseStop()))
            .ToList();
        var succeeded = await Task.WhenAll(work.Select(item => PerformAsync(item.Person, item.Action, item.Stop, cancellationToken)));

        var finished = time.GetUtcNow();
        for (var i = 0; i < work.Count; i++)
        {
            Record(work[i].Person, work[i].Action, work[i].Stop, succeeded[i], finished);
        }

        if (finished >= _nextSummaryAt)
        {
            var joined = _crowd.Count(person => person.StopId is not null);
            logger.Summary(joined, _moves, _reads, _errors);
            _nextSummaryAt = finished + SummaryInterval;
        }
    }

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
            _crowd = Crowd.Create(_options, picker, random, now);
            _nextSummaryAt = now + SummaryInterval;
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
