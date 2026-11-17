using GleisGrain.Alerts.Contracts;
using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Alerts.Domain;

/// <summary>A service notice on the board. It keeps the time it was first seen while its content follows the source.</summary>
internal sealed class Alert(
    AlertId id,
    string title,
    string text,
    IReadOnlyList<string> lines,
    IReadOnlyList<string> stopIds,
    DateTimeOffset firstSeenAt)
{
    public AlertId Id { get; } = id;

    public string Title { get; } = title;

    public string Text { get; } = text;

    public IReadOnlyList<string> Lines { get; } = lines;

    public IReadOnlyList<string> StopIds { get; } = stopIds;

    public DateTimeOffset FirstSeenAt { get; } = firstSeenAt;

    public static Alert From(NoticeSnapshot notice, DateTimeOffset now) =>
        new(AlertIds.From(notice.Id), notice.Title, notice.Text, notice.Lines, notice.StopIds, now);

    /// <summary>Takes over the content of the notice. Returns this alert when nothing changed.</summary>
    public Alert Update(NoticeSnapshot notice) =>
        Title == notice.Title && Text == notice.Text && Lines.SequenceEqual(notice.Lines) && StopIds.SequenceEqual(notice.StopIds)
            ? this
            : new Alert(Id, notice.Title, notice.Text, notice.Lines, notice.StopIds, FirstSeenAt);

    /// <summary>Does the alert name this line and this stop? A filter that is not given matches everything.</summary>
    public bool Matches(string? line, string? stopId) =>
        (string.IsNullOrWhiteSpace(line) || Lines.Any(own => LineNames.AreSame(own, line)))
        && (string.IsNullOrWhiteSpace(stopId) || StopIds.Contains(stopId.Trim(), StringComparer.Ordinal));

    public AlertSnapshot ToSnapshot() => new(Id, Title, Text, Lines, StopIds, FirstSeenAt);
}
