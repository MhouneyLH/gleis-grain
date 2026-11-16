namespace GleisGrain.Ingestion.Contracts;

/// <summary>A service notice (detour, stop relocation, extra service). Lines and stop ids are empty when the notice names none.</summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Ingestion.Contracts.NoticeSnapshot")]
public sealed record NoticeSnapshot(
    [property: Id(0)] string Id,
    [property: Id(1)] string Title,
    [property: Id(2)] string Text,
    [property: Id(3)] IReadOnlyList<string> Lines,
    [property: Id(4)] IReadOnlyList<string> StopIds);
