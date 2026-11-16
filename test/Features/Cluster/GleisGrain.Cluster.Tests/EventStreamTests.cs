using GleisGrain.Cluster.Contracts;
using GleisGrain.Cluster.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace GleisGrain.Cluster.Tests;

/// <summary>The server-sent event stream over a real connection, fed through the hub grain.</summary>
public sealed class EventStreamTests
{
    [Fact]
    public async Task StreamEvents_AfterAPublish_WritesTheEventAndItsJsonData()
    {
        await using var app = await ClusterApp.StartAsync(enableChaos: false);
        var liveEvent = LiveEvent.Of(new StopWatchersChanged("8f3a1c", 12));
        var hub = app.Services.GetRequiredService<IGrainFactory>().GetGrain<IClusterEventsGrain>(ClusterEventKeys.Hub);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        using var response = await app.Client.GetAsync("/api/cluster/events", HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(timeout.Token));
        var lines = ReadUntilEventAsync(reader, timeout.Token);

        // The silo subscribes itself to the hub in the background right after it starts, so publish until the event shows.
        while (!lines.IsCompleted)
        {
            await hub.PublishAsync(liveEvent);
            await Task.WhenAny(lines, Task.Delay(100, timeout.Token));
        }

        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["event: stop.watchers", $"data: {liveEvent.Json}"], await lines);
    }

    /// <summary>Reads lines up to the end of the first event, skipping comment lines.</summary>
    private static async Task<List<string>> ReadUntilEventAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var lines = new List<string>();
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith(':') || (line.Length == 0 && lines.Count == 0))
            {
                continue;
            }

            if (line.Length == 0)
            {
                break;
            }

            lines.Add(line);
        }

        return lines;
    }
}
