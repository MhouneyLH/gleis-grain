using GleisGrain.Game.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>Pure domain model: the score of every player who ever scored. Nicknames are compared without regard to case.</summary>
internal sealed class Leaderboard
{
    /// <summary>How many players the ranking shows.</summary>
    public const int TopSize = 10;

    private readonly Dictionary<string, LeaderboardEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public Leaderboard(IEnumerable<LeaderboardEntry>? entries = null)
    {
        foreach (var entry in entries ?? [])
        {
            _entries[entry.Nickname] = entry;
        }
    }

    public IReadOnlyCollection<LeaderboardEntry> Entries => _entries.Values;

    /// <summary>
    /// Raises the score of a player. Scores only grow, so a lower or equal score is ignored: reports are one-way
    /// calls and may arrive out of order. False when nothing changed.
    /// </summary>
    public bool Report(string nickname, int score)
    {
        if (_entries.TryGetValue(nickname, out var current) && current.Score >= score)
        {
            return false;
        }

        _entries[nickname] = new LeaderboardEntry(nickname, score);

        return true;
    }

    /// <summary>The best players, highest score first, ties by nickname.</summary>
    public IReadOnlyList<LeaderboardEntry> Top(int count) =>
        [.. _entries.Values
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Nickname, StringComparer.OrdinalIgnoreCase)
            .Take(count)];
}
