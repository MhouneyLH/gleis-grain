namespace GleisGrain.Alerts.Contracts;

[GenerateSerializer, Immutable, Alias("GleisGrain.Alerts.Contracts.AlertSnapshot")]
public sealed record AlertSnapshot(
    [property: Id(0)] AlertId Id,
    [property: Id(1)] string Title,
    [property: Id(2)] string Text,
    [property: Id(3)] IReadOnlyList<string> Lines,
    [property: Id(4)] IReadOnlyList<string> StopIds,
    [property: Id(5)] DateTimeOffset FirstSeenAt);
