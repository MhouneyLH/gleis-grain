using System.Net;
using System.Net.Sockets;
using GleisGrain.Common.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;

namespace GleisGrain.Alerts.Tests;

/// <summary>
/// A web host with one silo and the Alerts module wired in: in-memory storage and reminders, free ports,
/// the fake source grain of this assembly instead of Ingestion, and a shiftable clock.
/// </summary>
internal static class AlertsTestHost
{
    public static async Task<WebApplication> StartAsync(ShiftableClock? time = null, TimeSpan? reminderPeriod = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{FreePort()}");
        builder.Configuration["Alerts:ReminderPeriod"] = (reminderPeriod ?? TimeSpan.FromMinutes(1)).ToString();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        if (time is not null)
        {
            builder.Services.AddSingleton<TimeProvider>(time);
        }

        builder.AddAlertsModule();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: FreePort());
            silo.Configure<ClusterOptions>(cluster =>
            {
                cluster.ClusterId = $"alerts-tests-{Guid.NewGuid():N}";
                cluster.ServiceId = $"alerts-tests-{Guid.NewGuid():N}";
            });
            silo.AddMemoryGrainStorage(AlertsModule.StorageName);
            silo.UseInMemoryReminderService();
        });

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGroup("/api").MapAlertsEndpoints();
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
