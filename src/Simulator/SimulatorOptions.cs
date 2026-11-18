using System.ComponentModel.DataAnnotations;

namespace GleisGrain.Simulator;

/// <summary>The <c>Simulator</c> configuration section.</summary>
public sealed class SimulatorOptions
{
    public const string SectionName = "Simulator";

    /// <summary>Where the public API lives. Under Aspire this is the url of the gateway.</summary>
    [Required]
    public string BaseUrl { get; set; } = "http://localhost:5080";

    /// <summary>How many virtual people join.</summary>
    [Range(1, 9999)]
    public int People { get; set; } = 80;

    /// <summary>The people join evenly spread over this time.</summary>
    [Range(0, 3600)]
    public int RampUpSeconds { get; set; } = 40;

    /// <summary>The share of stop picks that go to Hauptbahnhof.</summary>
    [Range(0.0, 1.0)]
    public double HotStopShare { get; set; } = 0.4;

    /// <summary>Fixed seed for reproducible behaviour. Random when not set.</summary>
    public int? Seed { get; set; }
}
