# gleis-grain

Showcase for Microsoft Orleans built on public transport data from Linz AG. Stops, trips and service notices are virtual actors, kept up to date with reminders and timers.

The project is the demo for a talk about the actor paradigm at the .NET Stammtisch Linz.

> Work in progress. Right now this is only the scaffold: four modules with one example endpoint each. The XML client and the real grains are still missing.

## Data source

Linz AG runs an EFA server. Requests go to `https://www.linzag.at/static/XML_TRIP_REQUEST2` and the answer is XML. A call without parameters already returns the current service notices (stop relocations, detours, extra services), which the Alerts context will use. Departures should come from the departure monitor request of the same server.

Not checked yet: terms of use and rate limits. Live calls are the plan, so keep the polling polite.

## Getting Started

Everything runs in the devcontainer, so you only need VS Code with the Dev Containers extension and Docker on the host.

1. Open the repo in VS Code and choose "Reopen in Container".
2. Start the AppHost: `dotnet run --project src/AppHost`
3. The Aspire dashboard is on http://localhost:15000. It shows Postgres, pgAdmin and the one `gleis-grain` service that contains all modules.
4. The service runs on http://localhost:5080:
   - `/dashboard` is the Orleans Dashboard: silos, grains, activations and request rates. It is switched on with `Orleans:EnableDashboard` in `src/Host/appsettings.json` and off by default in code, because it exposes internals of the cluster.
   - `/scalar` has the API docs.

The Orleans SQL schema is not loaded into the databases yet, so the services will crash on startup until that is done.

Run the tests with `dotnet test --solution gleis-grain.slnx`.

## Bounded contexts

| Context   | Aggregate | What it owns                                                    |
|-----------|-----------|-----------------------------------------------------------------|
| Stops     | Stop      | A stop and its departure board, refreshed by a reminder         |
| Trips     | Trip      | A trip with its delay state                                     |
| Alerts    | Alert     | Service notices and disruptions                                 |
| Ingestion | Source    | The EFA XML client, parsing and rate limiting                   |

Planned calls between them: Stops and Alerts fetch through Ingestion, Trips read from Stops.

## Layout

This is a modular monolith: one deployable (`src/Host`) that contains all bounded contexts as modules.

```
src/
  AppHost/            Aspire host, starts Postgres and the Host
  Host/               the one deployable: composition root, calls every module
  Common/             options, HTTP, OpenTelemetry and Orleans setup shared by all modules
  Common.Abstractions/ tiny shared base for strongly typed ids, safe to reference from Contracts
  Features/<Module>/
    GleisGrain.<Module>/            the module, a class library
      <Module>Module.cs             its only public type: Add...Module and Map...Endpoints
      Endpoints/                    one file per endpoint: request, response, handler
      Domain/                       domain model, persisted state, grain
      <Module>Options.cs
      Metrics.cs  Logs.cs  Errors.cs
    GleisGrain.<Module>.Contracts/  grain interfaces, snapshots and the module's ids
test/                 mirrors src, plus test/Modules for boundary and composition tests
docs/                 architecture diagram and talk notes
```

Rules that keep it modular (checked by `test/Modules`):
- A module exposes only its `<Module>Module` class, everything else is `internal`.
- Modules call each other only through the other module's Contracts project, never its internals.
- Contracts do not reference modules, `Common` or ASP.NET Core.
- Every module has its own database, options section, metrics and tests.

Adding a module means: one project and its Contracts, one `Add...Module` and one `Map...Endpoints` call in `src/Host/Program.cs`, and its storage and meter name in the same file.

Splitting back into several deployables is possible later: a small host per module that calls only its own `Add...Module` and `Map...Endpoints`.

## Orleans and the domain model

The domain class (for example `Stop`) is plain C# and knows nothing about Orleans. The grain is a thin wrapper that loads the state, calls the domain class and writes the state back. That keeps the business rules testable without a silo.

API and silo run in the same process, so endpoints call `IGrainFactory` directly. The host is one silo with the grains of every module, and several replicas of it form a cluster of identical silos.

Modules talk to each other through grain calls on their Contracts interfaces. Cluster membership and reminders live in the shared `orleans` database. Grain state lives in each module's own database.

## Strongly typed ids

Every aggregate has its own id type, for example `StopId`. They are `readonly record struct`s, so a `StopId` cannot be passed where a `TripId` is expected. The value is the id the EFA server uses.

- Each id is defined in the Contracts project of the context that owns it.
- Common.Abstractions only holds the shared pieces: the `IStronglyTypedId` interface and a JSON converter. Common adds an OpenAPI transformer, so ids show up as plain strings in the API docs.
- Ids parse from routes through `IParsable`, so an invalid id is rejected with a 400 before the handler runs.
- The grain key is the id value (`IGrainWithStringKey`).

## Releases

Releases are cut by pushing a tag on `main`. The release workflow checks the tag, builds and tests with that version and creates a GitHub release with generated notes.

- `1.2.3` is a release.
- `1.2.3-rc.1` is a release candidate and is marked as pre-release.

```
git tag 1.2.3-rc.1
git push origin 1.2.3-rc.1
```

Architecture diagram: [docs/architecture.md](docs/architecture.md).

## Development

- .NET SDK 10.0.402, pinned in `global.json` without roll forward.
- Package versions are managed centrally in `Directory.Packages.props` and pinned exactly. Each project has a `packages.lock.json`, and CI restores in locked mode.
- Warnings are errors. Style and analyzer rules are in `.editorconfig`.
- Postgres runs as one container with one database per context plus `orleans`.
- Tests use xunit.v3. Integration tests will later use the AppHost.
- CI runs only the modules that were touched, plus the boundary and composition tests. A change to Common, build props or the workflows runs all of them.
