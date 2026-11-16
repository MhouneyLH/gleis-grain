using Orleans.Concurrency;

namespace GleisGrain.Game.Contracts;

/// <summary>The ranking of all players. One instance for the whole game.</summary>
/// <remarks>The grain key is <see cref="LeaderboardKeys.Main"/>.</remarks>
[Alias("GleisGrain.Game.Contracts.ILeaderboardGrain")]
public interface ILeaderboardGrain : IGrainWithStringKey
{
    /// <summary>A player reports their new score. Fire and forget.</summary>
    [OneWay, Alias("ReportScoreAsync")]
    Task ReportScoreAsync(string nickname, int score);

    /// <summary>The best players, highest score first, ties by nickname.</summary>
    [Alias("GetTopAsync")]
    Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(int count);
}

public static class LeaderboardKeys
{
    public const string Main = "main";
}

[GenerateSerializer, Immutable, Alias("GleisGrain.Game.Contracts.LeaderboardEntry")]
public sealed record LeaderboardEntry([property: Id(0)] string Nickname, [property: Id(1)] int Score);
