using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using GleisGrain.Common.Abstractions;

namespace GleisGrain.Game.Contracts;

/// <summary>
/// A player's name and identity. Trimmed, 2 to 20 characters of letters, digits, space, dash and underscore.
/// Two nicknames are the same player when they only differ in case, so the grain key is <see cref="Key"/>.
/// </summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Game.Contracts.Nickname")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<Nickname>))]
public readonly record struct Nickname([property: Id(0)] string Value) : IStronglyTypedId<Nickname, string>
{
    public const int MinLength = 2;
    public const int MaxLength = 20;

    /// <summary>The grain key: the nickname in lower case.</summary>
    [JsonIgnore]
    public string Key => Value.ToLowerInvariant();

    public static Nickname From(string value) =>
        TryParse(value, null, out var id) ? id : throw new ArgumentException($"'{value}' is not a valid nickname.", nameof(value));

    public static Nickname Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var id) ? id : throw new FormatException($"'{s}' is not a valid nickname.");

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Nickname result)
    {
        var trimmed = s?.Trim();
        if (trimmed is { Length: >= MinLength and <= MaxLength } && trimmed.All(c => char.IsAsciiLetterOrDigit(c) || c is ' ' or '-' or '_'))
        {
            result = new Nickname(trimmed);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value ?? string.Empty;
}
