namespace GleisGrain.Game.Domain;

/// <summary>How many points a guess earns: 3 for the exact delay, 1 for one minute off, otherwise 0.</summary>
internal static class Scoring
{
    public const int ExactPoints = 3;
    public const int CloseCallPoints = 1;

    public static int Points(int guessedDelayMinutes, int actualDelayMinutes) =>
        Math.Abs(guessedDelayMinutes - actualDelayMinutes) switch
        {
            0 => ExactPoints,
            1 => CloseCallPoints,
            _ => 0,
        };
}
