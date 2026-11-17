namespace GleisGrain.Alerts.Domain;

internal static class LineNames
{
    private static readonly string[] _prefixes = ["LINIE", "L"];

    /// <summary>Line names are compared tolerantly: "26", "L 26", "l26" and "Linie 26" are the same line.</summary>
    public static bool AreSame(string a, string b) => Normalize(a) == Normalize(b);

    private static string Normalize(string line)
    {
        var compact = new string([.. line.Where(char.IsLetterOrDigit)]).ToUpperInvariant();

        foreach (var prefix in _prefixes)
        {
            if (compact.StartsWith(prefix, StringComparison.Ordinal) && compact.Length > prefix.Length && char.IsDigit(compact[prefix.Length]))
            {
                return compact[prefix.Length..];
            }
        }

        return compact;
    }
}
