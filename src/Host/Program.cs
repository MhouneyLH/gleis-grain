using GleisGrain.Alerts;
using GleisGrain.Common;
using GleisGrain.Ingestion;
using GleisGrain.Stops;
using GleisGrain.Trips;

// Composition root of the modular monolith: one process, one Orleans silo, four modules.
// A module is added by one Add...Module call, one Map...Endpoints call and its storage and meter name.
var builder = WebApplication.CreateBuilder(args);

builder.AddGleisGrainObservability(
    "gleisgrain",
    StopsModule.MeterName,
    TripsModule.MeterName,
    AlertsModule.MeterName,
    IngestionModule.MeterName);
builder.AddGleisGrainHttp();
builder.AddGleisGrainOrleans(
    StopsModule.StorageName,
    TripsModule.StorageName,
    AlertsModule.StorageName,
    IngestionModule.StorageName);

builder
    .AddStopsModule()
    .AddTripsModule()
    .AddAlertsModule()
    .AddIngestionModule();

var app = builder.Build();

app.UseGleisGrainHttp();
app.MapGleisGrainDashboard();

app.MapStopsEndpoints()
    .MapTripsEndpoints()
    .MapAlertsEndpoints()
    .MapIngestionEndpoints();

await app.RunAsync();
