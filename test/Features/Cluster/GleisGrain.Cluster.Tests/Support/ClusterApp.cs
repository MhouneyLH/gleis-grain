using System.Net;
using System.Net.Sockets;
using GleisGrain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GleisGrain.Cluster.Tests.Support;

/// <summary>A real Kestrel server with one real silo, the Cluster module and the fake grains, as the host wires it.</summary>
internal sealed class ClusterApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ClusterApp(WebApplication app, FakeProcessKiller killer)
    {
        _app = app;
        Killer = killer;
        Client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
    }

    public HttpClient Client { get; }

    public FakeProcessKiller Killer { get; }

    public IServiceProvider Services => _app.Services;

    public static async Task<ClusterApp> StartAsync(bool enableChaos)
    {
        var killer = new FakeProcessKiller();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{FreePort()}");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cluster:EnableChaos"] = enableChaos.ToString(),
        });
        builder.AddGleisGrainHttp();
        builder.UseOrleans(silo => silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: FreePort()));
        builder.AddClusterModule();
        builder.Services.AddSingleton<IProcessKiller>(killer);

        var app = builder.Build();
        app.UseGleisGrainHttp();
        app.MapGroup("/api").MapClusterEndpoints();
        await app.StartAsync();

        return new ClusterApp(app, killer);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
