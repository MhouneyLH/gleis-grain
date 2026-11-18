using System.Globalization;

namespace GleisGrain.Common;

/// <summary>
/// Silo names have the form <c>v&lt;grain version&gt;-&lt;5 random hex chars&gt;</c>, for example <c>v2-b72c0</c>.
/// The cluster endpoints and live events carry the name, so the frontend reads the grain version from the prefix.
/// </summary>
public static class SiloNames
{
    private const int SuffixBits = 20;

    public static string Create(int grainVersion) => Create(grainVersion, Random.Shared.Next(1 << SuffixBits));

    /// <summary>Builds the name from a given random value, which makes the format testable.</summary>
    public static string Create(int grainVersion, int random)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(grainVersion, 1);

        return string.Create(CultureInfo.InvariantCulture, $"v{grainVersion}-{random & ((1 << SuffixBits) - 1):x5}");
    }

    /// <summary>The grain version in the name prefix, or null when the name does not follow the format.</summary>
    public static int? ParseVersion(string? siloName)
    {
        var dash = siloName?.IndexOf('-', StringComparison.Ordinal) ?? -1;

        return siloName is not null && siloName.StartsWith('v') && dash > 1
            && int.TryParse(siloName.AsSpan(1, dash - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var version)
            ? version
            : null;
    }
}
