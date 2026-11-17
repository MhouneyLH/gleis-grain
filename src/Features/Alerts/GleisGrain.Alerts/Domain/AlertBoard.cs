using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Alerts.Domain;

/// <summary>All current service notices. A refresh adds new ones, updates known ones and drops the ones that are gone.</summary>
internal sealed class AlertBoard(IEnumerable<Alert>? alerts = null)
{
    private List<Alert> _alerts = [.. alerts ?? []];

    public IReadOnlyList<Alert> Alerts => _alerts;

    /// <summary>When the board last took over notices. Null for a board that never refreshed, for example after a restart.</summary>
    public DateTimeOffset? RefreshedAt { get; private set; }

    public bool NeedsRefresh(DateTimeOffset now, TimeSpan staleAfter) => RefreshedAt is null || now - RefreshedAt >= staleAfter;

    /// <summary>Takes over the notices. Returns whether the alerts on the board changed.</summary>
    public bool Refresh(IReadOnlyList<NoticeSnapshot> notices, DateTimeOffset now)
    {
        var known = _alerts.ToDictionary(alert => alert.Id);
        var refreshed = new List<Alert>();

        foreach (var notice in notices)
        {
            var id = AlertIds.From(notice.Id);
            if (refreshed.Exists(alert => alert.Id == id))
            {
                continue;
            }

            refreshed.Add(known.TryGetValue(id, out var alert) ? alert.Update(notice) : Alert.From(notice, now));
        }

        // Update hands back the same instance for unchanged content, so reference equality tells what changed.
        var changed = !refreshed.SequenceEqual(_alerts);
        _alerts = refreshed;
        RefreshedAt = now;

        return changed;
    }

    public IReadOnlyList<Alert> Filter(string? line, string? stopId) =>
        _alerts.Where(alert => alert.Matches(line, stopId)).ToList();
}
