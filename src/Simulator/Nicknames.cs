namespace GleisGrain.Simulator;

/// <summary>The nicknames of the virtual people. They satisfy the nickname rules of the game: 2 to 20 letters, digits, space, dash, underscore.</summary>
public static class Nicknames
{
    /// <summary>The nickname of the person with the given number, counted from 1: <c>Sim-001</c>.</summary>
    public static string For(int number) => $"Sim-{number:D3}";
}
