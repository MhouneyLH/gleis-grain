using System.Net;
using System.Net.Sockets;
using GleisGrain.Alerts;
using GleisGrain.Alerts.Contracts;
using GleisGrain.Cluster;
using GleisGrain.Common;
using GleisGrain.Game;
using GleisGrain.Game.Contracts;
using GleisGrain.Ingestion;
using GleisGrain.Stops;
using GleisGrain.Stops.Contracts;
using GleisGrain.Trips;
using GleisGrain.Trips.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;

namespace GleisGrain.Modules.Tests;

/// <summary>
/// Composes all six modules into one silo the way the host does, with in-memory storage, replayed Linz AG data and an
/// in-memory metric exporter, and drives one round through every module: a board, notices, a player with a bet.
/// </summary>
public sealed class ModuleCompositionTests : IAsyncLifetime
{
    private const string Hauptbahnhof = "60501720";

    private static readonly string[] _meterNames =
    [
        StopsModule.MeterName,
        TripsModule.MeterName,
        AlertsModule.MeterName,
        IngestionModule.MeterName,
        GameModule.MeterName,
        ClusterModule.MeterName,
    ];

    private readonly List<Metric> _exported = [];
    private IHost? _host;

    public async ValueTask InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Ingestion:Mode"] = "Replay" });

        builder.AddGleisGrainObservability("composition-test", _meterNames);
        builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddInMemoryExporter(_exported));

        builder
            .AddStopsModule()
            .AddTripsModule()
            .AddAlertsModule()
            .AddIngestionModule()
            .AddGameModule()
            .AddClusterModule();

        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: FreePort());
            silo.UseInMemoryReminderService();
            foreach (var storage in new[] { StopsModule.StorageName, TripsModule.StorageName, AlertsModule.StorageName, IngestionModule.StorageName, GameModule.StorageName })
            {
                silo.AddMemoryGrainStorage(storage);
            }
        });

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Fact]
    public async Task APlayerWatchesAStopAndBetsOnADeparture_AcrossAllModules()
    {
        var grains = _host!.Services.GetRequiredService<IGrainFactory>();
        var board = await grains.GetGrain<IStopGrain>(Hauptbahnhof).GetBoardAsync();
        Assert.NotNull(board);
        Assert.NotEmpty(board.Departures);
        var player = grains.GetGrain<IPlayerGrain>(Nickname.From("Max").Key);
        Assert.True(await player.ClaimAsync(Nickname.From("Max")));
        Assert.Equal(WatchStopOutcome.Watching, await player.WatchStopAsync(StopId.From(Hauptbahnhof)));
        var departure = board.Departures[0];

        var outcome = await PlaceBetWhenTheTripIsKnownAsync(grains, player, departure.TripId);

        Assert.Equal(PlaceBetOutcome.Placed, outcome);
        var snapshot = await player.GetAsync();
        Assert.Equal(departure.TripId, snapshot?.OpenBet?.TripId);
        Assert.Equal(StopId.From(Hauptbahnhof), snapshot?.WatchedStop);
        Assert.Equal(1, (await grains.GetGrain<IStopGrain>(Hauptbahnhof).GetBoardAsync())?.Watchers);
    }

    [Fact]
    public async Task EveryModulesMeter_IsRegisteredAndExported()
    {
        var grains = _host!.Services.GetRequiredService<IGrainFactory>();
        await grains.GetGrain<IStopGrain>(Hauptbahnhof).GetBoardAsync();
        await grains.GetGrain<IAlertBoardGrain>(AlertBoardKeys.Linz).GetAsync(null, null);
        await grains.GetGrain<IPlayerGrain>(Nickname.From("Meter").Key).ClaimAsync(Nickname.From("Meter"));

        await WaitUntilAsync(() =>
        {
            _host!.Services.GetRequiredService<MeterProvider>().ForceFlush();
            lock (_exported)
            {
                return _meterNames.All(name => _exported.Any(metric => metric.MeterName == name));
            }
        });

        string[] exportedMeters;
        lock (_exported)
        {
            exportedMeters = [.. _exported.Select(metric => metric.MeterName).Distinct()];
        }

        Assert.All(_meterNames, name => Assert.Contains(name, exportedMeters));
    }

    // The stop reports its trips with fire-and-forget calls, so the trip can be a moment behind the board.
    private static async Task<PlaceBetOutcome> PlaceBetWhenTheTripIsKnownAsync(IGrainFactory grains, IPlayerGrain player, TripId tripId)
    {
        PlaceBetOutcome outcome = PlaceBetOutcome.UnknownTrip;
        await WaitUntilAsync(async () =>
        {
            outcome = await player.PlaceBetAsync(tripId, 2);
            return outcome != PlaceBetOutcome.UnknownTrip;
        });

        return outcome;
    }

    private static async Task WaitUntilAsync(Func<bool> condition) =>
        await WaitUntilAsync(() => Task.FromResult(condition()));

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!await condition())
        {
            await Task.Delay(100, timeout.Token);
        }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
