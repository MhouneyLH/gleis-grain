using GleisGrain.Alerts.Contracts;

namespace GleisGrain.Alerts.Domain;

/// <summary>
/// Persisted blob. Only add fields with new ids; bump <see cref="CurrentSchemaVersion"/> and migrate on activation.
/// Only the alerts are stored: the time of the last refresh is not, so a restarted board refreshes on first use.
/// </summary>
[GenerateSerializer, Alias("GleisGrain.Alerts.Domain.AlertBoardState")]
internal sealed class AlertBoardState
{
    public const int CurrentSchemaVersion = 1;

    [Id(0)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [Id(1)]
    public List<AlertState> Alerts { get; set; } = [];

    public static AlertBoardState From(AlertBoard board) => new()
    {
        Alerts = [.. board.Alerts.Select(alert => new AlertState
        {
            Id = alert.Id.Value,
            Title = alert.Title,
            Text = alert.Text,
            Lines = [.. alert.Lines],
            StopIds = [.. alert.StopIds],
            FirstSeenAt = alert.FirstSeenAt,
        })],
    };

    public AlertBoard ToBoard() =>
        new(Alerts.Select(alert => new Alert(AlertId.From(alert.Id), alert.Title, alert.Text, alert.Lines, alert.StopIds, alert.FirstSeenAt)));
}

[GenerateSerializer, Alias("GleisGrain.Alerts.Domain.AlertState")]
internal sealed class AlertState
{
    [Id(0)]
    public string Id { get; set; } = string.Empty;

    [Id(1)]
    public string Title { get; set; } = string.Empty;

    [Id(2)]
    public string Text { get; set; } = string.Empty;

    [Id(3)]
    public List<string> Lines { get; set; } = [];

    [Id(4)]
    public List<string> StopIds { get; set; } = [];

    [Id(5)]
    public DateTimeOffset FirstSeenAt { get; set; }
}
