using GleisGrain.Trips.Contracts;

namespace GleisGrain.Game.Domain;

/// <summary>A guess of the delay of one departure, in whole minutes. 0 means on time.</summary>
internal sealed record Bet
{
    public const int MinGuess = 0;
    public const int MaxGuess = 15;

    public Bet(TripId tripId, string stopId, string line, DateTimeOffset plannedAt, int guessedDelayMinutes)
    {
        if (!IsValidGuess(guessedDelayMinutes))
        {
            throw new ArgumentOutOfRangeException(nameof(guessedDelayMinutes), guessedDelayMinutes, $"A guess is {MinGuess} to {MaxGuess} minutes.");
        }

        TripId = tripId;
        StopId = stopId;
        Line = line;
        PlannedAt = plannedAt;
        GuessedDelayMinutes = guessedDelayMinutes;
    }

    public TripId TripId { get; }

    public string StopId { get; }

    public string Line { get; }

    public DateTimeOffset PlannedAt { get; }

    public int GuessedDelayMinutes { get; }

    public static bool IsValidGuess(int minutes) => minutes is >= MinGuess and <= MaxGuess;
}
