using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Ingestion;

/// <summary>Where the EFA answers come from.</summary>
internal enum IngestionMode
{
    /// <summary>Call the Linz AG server.</summary>
    Live,

    /// <summary>Serve the recorded answers shipped with the module, so the demo survives a downtime of Linz AG.</summary>
    Replay,
}

internal sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    /// <summary>Grain storage name; equals the connection string name of the ingestion database.</summary>
    public const string StorageName = "ingestion";

    [EnumDataType(typeof(IngestionMode))]
    public IngestionMode Mode { get; init; } = IngestionMode.Live;

    [Required, Url]
    public string BaseUrl { get; init; } = "https://www.linzag.at/static/";

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>The shortest time between two calls to the server, for the whole cluster.</summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:01:00")]
    public TimeSpan MinimumCallInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
    public TimeSpan DepartureCacheTtl { get; init; } = TimeSpan.FromSeconds(20);

    [Range(typeof(TimeSpan), "00:00:00", "01:00:00")]
    public TimeSpan NoticeCacheTtl { get; init; } = TimeSpan.FromSeconds(60);
}
