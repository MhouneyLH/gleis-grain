using System.Text.Json;

namespace GleisGrain.Cluster.Contracts;

/// <summary>An event for the browsers: a type name and a JSON payload. The type is the server-sent event name.</summary>
[GenerateSerializer, Immutable, Alias("GleisGrain.Cluster.Contracts.LiveEvent")]
public sealed record LiveEvent([property: Id(0)] string Type, [property: Id(1)] string Json)
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static LiveEvent Of<TPayload>(TPayload payload)
        where TPayload : ILivePayload =>
        new(TPayload.EventType, JsonSerializer.Serialize(payload, _json));
}

/// <summary>A payload that knows its event type.</summary>
public interface ILivePayload
{
    static abstract string EventType { get; }
}
