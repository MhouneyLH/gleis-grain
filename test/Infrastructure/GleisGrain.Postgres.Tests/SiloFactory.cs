using System.Globalization;
using System.Net;
using System.Net.Sockets;
using GleisGrain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GleisGrain.Postgres.Tests;

/// <summary>Starts one silo the way the Host does, against the Postgres container.</summary>
internal static class SiloFactory
{
    /// <summary>The grain storage name the test grains use, as a module would.</summary>
    public const string StorageName = "stops";

    /// <summary>
    /// Builds and starts a silo with <c>AddGleisGrainOrleans</c>. Every test uses its own cluster id,
    /// so the tests do not see each other's rows. The caller disposes the host.
    /// </summary>
    public static async Task<IHost> StartAsync(PostgresFixture postgres, string clusterId)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:orleans"] = postgres.ConnectionString("orleans"),
            [$"ConnectionStrings:{StorageName}"] = postgres.ConnectionString(StorageName),
            ["Orleans:ClusterId"] = clusterId,
            ["Orleans:ServiceId"] = clusterId,
            ["Orleans:SiloPort"] = FreePort().ToString(CultureInfo.InvariantCulture),
            ["Orleans:GatewayPort"] = FreePort().ToString(CultureInfo.InvariantCulture),
        });
        builder.Logging.ClearProviders();
        builder.AddGleisGrainOrleans(StorageName);

        // The default minimum is one minute, too slow for a test.
        builder.Services.Configure<ReminderOptions>(options => options.MinimumReminderPeriod = TimeSpan.FromSeconds(1));

        var host = builder.Build();
        await host.StartAsync();
        return host;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
