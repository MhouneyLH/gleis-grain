using System.Net;
using System.Net.Sockets;
using GleisGrain.Common;
using GleisGrain.Common.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans.Dashboard;

namespace GleisGrain.Modules.Tests;

/// <summary>The Orleans Dashboard answers on the configured path when it is enabled, and does not when it is off.</summary>
public class DashboardTests
{
    [Fact]
    public async Task Dashboard_WhenEnabled_AnswersOnTheConfiguredPath()
    {
        await using var app = await StartAsync(enabled: true);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var response = await client.GetAsync("/dashboard/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_WhenDisabled_IsNotMapped()
    {
        await using var app = await StartAsync(enabled: false);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };

        var response = await client.GetAsync("/dashboard/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> StartAsync(bool enabled)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{FreePort()}");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Orleans:EnableDashboard"] = enabled.ToString(),
        });
        builder.Services.AddValidatedOptions<OrleansOptions>(OrleansOptions.SectionName);
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: FreePort());
            if (enabled)
            {
                silo.AddDashboard();
            }
        });

        var app = builder.Build();
        app.MapGleisGrainDashboard();
        await app.StartAsync();

        return app;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
