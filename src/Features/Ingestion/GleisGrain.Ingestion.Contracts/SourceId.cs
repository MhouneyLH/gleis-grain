using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using GleisGrain.Common.Abstractions;

namespace GleisGrain.Ingestion.Contracts;

[GenerateSerializer, Immutable, Alias("GleisGrain.Ingestion.Contracts.SourceId")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<SourceId>))]
public readonly record struct SourceId([property: Id(0)] string Value) : IStronglyTypedId<SourceId, string>
{
    public const int MaxLength = 64;

    /// <summary>The Linz AG EFA server, the only source for now.</summary>
    public static SourceId LinzAg => new("linz-ag");

    public static SourceId From(string value) =>
        TryParse(value, null, out var id) ? id : throw new ArgumentException($"'{value}' is not a valid Source id.", nameof(value));

    public static SourceId Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException($"'{s}' is not a valid Source id.");

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SourceId result)
    {
        if (!string.IsNullOrWhiteSpace(s) && s.Length <= MaxLength && s.All(c => char.IsAsciiLetterOrDigit(c) || c is ':' or '_' or '-' or '.'))
        {
            result = new SourceId(s);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value ?? string.Empty;
}
