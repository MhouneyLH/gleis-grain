using GleisGrain.Game.Contracts;
using GleisGrain.Stops.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>
/// Persisted blob. Only add fields with new ids; bump <see cref="CurrentSchemaVersion"/> and migrate on activation.
/// Ids 2 (score) and 4 (open bet) belonged to the removed betting game: never reuse them. A blob written with them still
/// loads, the serializer skips ids it does not know.
/// </summary>
[GenerateSerializer, Alias("GleisGrain.Game.Domain.PlayerState")]
internal sealed class PlayerState
{
    public const int CurrentSchemaVersion = 1;

    [Id(0)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [Id(1)]
    public string Nickname { get; set; } = string.Empty;

    [Id(3)]
    public string? WatchedStopId { get; set; }

    public static PlayerState From(Player player) => new()
    {
        Nickname = player.Nickname.Value,
        WatchedStopId = player.WatchedStop?.Value,
    };

    public Player ToPlayer() => new(new Nickname(Nickname), WatchedStopId is { } stop ? new StopId(stop) : null);
}
