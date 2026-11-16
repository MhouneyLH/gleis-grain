using System.Text;
using System.Threading.Channels;
using GleisGrain.Cluster.Events;
using Microsoft.AspNetCore.Http.Features;

namespace GleisGrain.Cluster.Endpoints;

/// <summary>
/// The server-sent event stream. Written by hand instead of <c>TypedResults.ServerSentEvents</c>, because that
/// cannot send the <c>: keep-alive</c> comment lines which keep proxies and phones from closing an idle stream.
/// </summary>
internal sealed class StreamEvents
{
    private static readonly TimeSpan _keepAliveEvery = TimeSpan.FromSeconds(15);

    public static void Map(IEndpointRouteBuilder routes) =>
        routes.MapGet("/events", Handle)
            .WithName("StreamEvents")
            .WithSummary("Streams live events as server-sent events.")
            .WithDescription("Part of the Cluster context: one `event:` and one JSON `data:` line per event, a `: keep-alive` comment every 15 seconds.")
            .Produces(StatusCodes.Status200OK, contentType: "text/event-stream")
            // The stream is meant to stay open, the default 30 second request timeout must not cut it.
            .DisableRequestTimeout();

    private static async Task Handle(HttpContext http, LiveEventBroadcaster broadcaster, ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<StreamEvents>();
        var response = http.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no"; // nginx: do not buffer the stream
        http.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        // Subscribe before the first byte goes out, so a client that sees the response headers misses no event.
        using var subscription = broadcaster.Subscribe();
        logger.EventClientConnected();

        try
        {
            await WriteAsync(response, ": connected\n\n", http.RequestAborted);

            while (true)
            {
                using var keepAlive = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
                keepAlive.CancelAfter(_keepAliveEvery);

                try
                {
                    var liveEvent = await subscription.Reader.ReadAsync(keepAlive.Token);
                    await WriteAsync(response, $"event: {liveEvent.Type}\ndata: {liveEvent.Json}\n\n", http.RequestAborted);
                }
                catch (OperationCanceledException) when (!http.RequestAborted.IsCancellationRequested)
                {
                    await WriteAsync(response, ": keep-alive\n\n", http.RequestAborted);
                }
            }
        }
        catch (Exception exception) when (exception is OperationCanceledException or ChannelClosedException)
        {
            // The browser went away or the silo is shutting down: the normal end of a stream.
        }
        finally
        {
            logger.EventClientDisconnected();
        }
    }

    private static async Task WriteAsync(HttpResponse response, string text, CancellationToken cancellationToken)
    {
        await response.Body.WriteAsync(Encoding.UTF8.GetBytes(text), cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
