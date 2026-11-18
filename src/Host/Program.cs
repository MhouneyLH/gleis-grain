using GleisGrain.Alerts;
using GleisGrain.Cluster;
using GleisGrain.Common;
using GleisGrain.Game;
using GleisGrain.Ingestion;
using GleisGrain.Stops;
using GleisGrain.Trips;

// Composition root of the modular monolith: one process, one Orleans silo, six modules.
// A module is added by one Add...Module call, one Map...Endpoints call and its storage and meter name.
var builder = WebApplication.CreateBuilder(args);

builder.AddGleisGrainObservability(
    "gleisgrain",
    StopsModule.MeterName,
    TripsModule.MeterName,
    AlertsModule.MeterName,
    IngestionModule.MeterName,
    GameModule.MeterName,
    ClusterModule.MeterName);
builder.AddGleisGrainHttp();
builder.AddGleisGrainOrleans(
    StopsModule.GrainVersion,
    StopsModule.StorageName,
    TripsModule.StorageName,
    AlertsModule.StorageName,
    IngestionModule.StorageName,
    GameModule.StorageName);

builder
    .AddStopsModule()
    .AddTripsModule()
    .AddAlertsModule()
    .AddIngestionModule()
    .AddGameModule()
    .AddClusterModule();

var app = builder.Build();

app.UseGleisGrainHttp();

// The built Vue app (npm run build in src/Frontend writes to wwwroot), so the host works without the Vite dev server.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGleisGrainDashboard();

// All module endpoints live under /api, so the frontend dev server proxies one prefix.
app.MapGroup("/api")
    .MapStopsEndpoints()
    .MapTripsEndpoints()
    .MapAlertsEndpoints()
    .MapIngestionEndpoints()
    .MapGameEndpoints()
    .MapClusterEndpoints();

// Deep links of the single page app (/projector, /stop/...) get index.html. The API keeps its own 404.
app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");

await app.RunAsync();
