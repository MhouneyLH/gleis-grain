using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using GleisGrain.Stops.Contracts;
using GleisGrain.Stops.Domain;

namespace GleisGrain.Stops.Catalogue;

/// <summary>The curated set of Linz stops, read once from the embedded <c>stops.json</c>.</summary>
internal sealed class StopCatalogue
{
    private const string ResourceName = "stops.json";

    private readonly Dictionary<StopId, StopInfo> _byId;

    private StopCatalogue(IReadOnlyList<StopInfo> stops)
    {
        All = stops;
        _byId = stops.ToDictionary(stop => stop.Id);
    }

    public IReadOnlyList<StopInfo> All { get; }

    public static StopCatalogue Load()
    {
        using var stream = typeof(StopCatalogue).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        var stops = JsonSerializer.Deserialize<List<StopInfo>>(stream, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is empty.");

        return new StopCatalogue(stops);
    }

    public bool TryGet(StopId id, [NotNullWhen(true)] out StopInfo? stop) => _byId.TryGetValue(id, out stop);
}
