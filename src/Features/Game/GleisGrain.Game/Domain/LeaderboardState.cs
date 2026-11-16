using GleisGrain.Game.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>Persisted blob. Only add fields with new ids; bump <see cref="CurrentSchemaVersion"/> and migrate on activation.</summary>
[GenerateSerializer, Alias("GleisGrain.Game.Domain.LeaderboardState")]
internal sealed class LeaderboardState
{
    public const int CurrentSchemaVersion = 1;

    [Id(0)]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Score by nickname.</summary>
    [Id(1)]
    public Dictionary<string, int> Scores { get; set; } = [];

    public static LeaderboardState From(Leaderboard leaderboard) => new()
    {
        Scores = leaderboard.Entries.ToDictionary(entry => entry.Nickname, entry => entry.Score),
    };

    public Leaderboard ToLeaderboard() =>
        new(Scores.Select(score => new LeaderboardEntry(score.Key, score.Value)));
}
