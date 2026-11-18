# Contributing

gleis-grain is the demo for a talk about Microsoft Orleans, so it has to stay easy to read. Small, clear changes are worth more than clever ones. The README explains what the project is and how to run it; this file explains how to work on it.

## Setup

Everything runs in the devcontainer. You need VS Code with the Dev Containers extension and Docker on the host, then "Reopen in Container".

Without the devcontainer you need the .NET SDK from `global.json` (pinned, no roll forward), Node 22.12 or newer for the frontend, and Docker for Postgres.

| Task | Command |
|---|---|
| Build | `dotnet build gleis-grain.slnx` |
| Test everything | `dotnet test --solution gleis-grain.slnx` |
| Test one project | `dotnet test --project test/Features/Stops/GleisGrain.Stops.Tests` |
| Run the whole demo | `dotnet run --project src/AppHost` (live data), or `--launch-profile replay` for recorded data |
| Try the API | the `.http` files in `src/Host/http` (VS Code REST Client, Rider, Visual Studio) |
| Frontend | `cd src/Frontend`, then `npm ci`, `npm run dev`, `npm run test`, `npm run build` |

The one test project that needs Docker is `test/Infrastructure/GleisGrain.Postgres.Tests`. It skips itself when Docker is missing.

## Running the demo

`dotnet run --project src/AppHost` starts everything. The Aspire dashboard on http://localhost:15000 lists every resource and its links:

- **Postgres and pgAdmin.** The password is fixed (`gleis-grain-dev`, local demo only), so the dev container and the host agree. If Postgres is unhealthy with `password authentication failed`, the data volume was created with another password: stop the AppHost and run `docker volume rm gleis-grain-postgres` once. The first start creates one database per context (`stops`, `trips`, `alerts`, `ingestion`, `game`) plus `orleans` and loads the Orleans schema from [db/](db/orleans/README.md). Postgres runs these scripts only on an empty data volume. After a schema change reset it with `docker volume rm gleis-grain-postgres`, which also deletes all data.
- **gleis-grain** is a YARP reverse proxy on http://localhost:5080 (override the port with `Gateway:Port`) in front of the two host builds below. **gleis-grain-v1** is the host in three replicas, **gleis-grain-v2** the same host built with `-p:GrainVersion=2` in three replicas that are not started automatically (see "Grain versioning demo").
- Each replica is one Orleans silo with its own silo and gateway port, assigned by Aspire. http://localhost:5080 load balances over the running replicas of both builds. The host serves:
  - `/dashboard` is the Orleans Dashboard: silos, grains, activations and request rates. It is on through `Orleans:EnableDashboard` in `src/Host/appsettings.json` and off by default in code, because it exposes internals of the cluster.
  - `/scalar` has the API docs, `/api/...` is the API.
- **simulator** (`src/Simulator`) plays the audience: virtual people `Sim-001`... join over `Simulator:RampUpSeconds` (40), pick stops (`Simulator:HotStopShare`, 0.4, goes to Hauptbahnhof), move every 20 to 60 seconds and read their board about every 15 seconds. It uses only the public HTTP API of the `gleis-grain` gateway, survives killed silos and is not started automatically: click start in the dashboard. Run it alone against a running host with `dotnet run --project src/Simulator` (`Simulator:BaseUrl`, default http://localhost:5080). It is also a small web app (`Simulator:Port`, 5090) with a control API to pin virtual people to stops (`Simulator:MaxPeople`, 500; `src/Simulator/http/simulator.http`); the frontend dev server gets its url as `SIMULATOR_HTTP`. Its tests: `dotnet test --project test/Simulator/GleisGrain.Simulator.Tests`.
- **frontend**, the Vite dev server of the Vue app with hot reload, shown as "Frontend". It proxies `/api` to the host.

Two launch profiles pick where departures come from: `live` (default) asks the Linz AG server, `replay` serves recorded responses, so the demo works offline and the server is never hit: `dotnet run --project src/AppHost --launch-profile replay`. The AppHost also turns the chaos button on (`Cluster:EnableChaos`).

### Grain versioning demo

`IStopGrain` carries `[Version(1)]`, or `[Version(2)]` when built with `-p:GrainVersion=2` (compile constant `GRAIN_V2` in the Stops projects). The AppHost builds the v2 host into `artifacts/v2` after every AppHost build (skip with `-p:SkipGrainV2Build=true`), so it does not touch the bin and obj folders of the normal build. The replicas never build: Aspire starts them with `dotnet run --no-build`, so build first (`dotnet run --project src/AppHost` does). To build it by hand:

```
dotnet build src/Host -p:GrainVersion=2 --artifacts-path artifacts/v2
```

Rollout on stage: start the three `gleis-grain-v2-...` replicas in the Aspire dashboard, wait until `/api/cluster/silos` lists six Active silos, then stop the three `gleis-grain-v1-...` replicas. The silo name is `v<version>-<random>`, see [docs/api.md](docs/api.md#grain-versioning). The same works with the Aspire CLI: `aspire resource <replica name> start`.

To see the cluster, open pgAdmin from the dashboard and run `SELECT * FROM orleansmembershiptable;`. Every AppHost start is a new cluster id, so filter on the newest `deploymentid`.

## Workflow

1. Open an issue first for anything bigger than a small fix, so we can agree on the idea. Use the issue templates.
2. Branch from `main`. Name the branch after the change: `feat/stops-board-cache`, `fix/game-leaderboard-order`.
3. Keep a pull request to one bounded context where you can. A change in `Common` or the build files runs every module's CI, so keep those separate.
4. Fill in the pull request template. CI must be green and the build free of warnings.

CI only runs the modules a change touches, plus the boundary tests in `test/Modules`. A change to `Common`, the build props or the workflows runs all of them. The simulator job runs when `src/Simulator` or `test/Simulator` changes. The frontend job runs when `src/Frontend` changes, the Postgres job when `db/` or `test/Infrastructure` changes.

## Commit messages

[Conventional Commits](https://www.conventionalcommits.org/), with the context as scope:

```
feat(stops): refresh the board while somebody watches
fix(game): keep the highest score when reports arrive out of order
docs(readme): explain the Orleans Dashboard link
```

- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `build`, `ci`, `chore`.
- Scopes: `stops`, `trips`, `alerts`, `ingestion`, `game`, `cluster`, `frontend`, `apphost`, `common`, plus `readme`, `requirements`, `talk` for docs.
- The subject is imperative and at most 72 characters. The body explains why, not what.
- A commit does one thing and leaves the build and the tests green.

## Code rules

The build is strict on purpose: warnings are errors, analyzers run at the Recommended level and `.editorconfig` is enforced during the build. Fix the cause instead of suppressing a warning.

### Domain model

- Each module is a bounded context with one aggregate per grain. The domain class (`Stop`, `Player`, ...) is plain C#: no Orleans, no I/O, time through `TimeProvider`. The grain is a thin wrapper that loads state, calls the domain, persists and publishes.
- Prefer the simplest thing that shows the idea. No abstraction without a second user.
- Every aggregate has a strongly typed id, a `readonly record struct` such as `StopId`, so a `StopId` cannot be passed where a `TripId` is expected. It is defined in the Contracts project of the owning module; the shared pieces (`IStronglyTypedId`, the JSON converter) are in `Common.Abstractions`.
- Ids parse from routes through `IParsable`, so an invalid id is a 400 before the handler runs. The grain key is the id value (`IGrainWithStringKey`).
- Expected failures are `DomainError` records thrown as `DomainException` in the endpoint handler. Never throw them across a grain call, return an outcome instead.

### Module boundaries

These are checked by the tests in `test/Modules`, so a violation fails CI:

- A module exposes only its `<Module>Module` class. Everything else is `internal`.
- Modules call each other only through the other module's Contracts project.
- Contracts reference no module, no `Common` and no ASP.NET Core.
- Every module has its own database, options section, meter and tests.

### Persisted state and contracts

Grain state lives as a blob in Postgres and survives deployments, so treat it like a public API:

- Every serialized type has `[GenerateSerializer]`, `[Alias("...")]` and stable `[Id(n)]` per field.
- Only add fields with new ids. Never reuse or retype an id.
- State classes carry a `SchemaVersion`. Bump it and migrate on activation when the meaning of a field changes.
- Do not persist every refresh. Keep refreshed data in memory and write it on a slow cadence.

### Logs, metrics and errors

- Logging uses `[LoggerMessage]` with named placeholders, so the fields arrive structured in the Aspire dashboard. EventId ranges: Stops 1000, Trips 2000, Alerts 3000, Ingestion 4000, Game 5000, Cluster 6000.
- Metrics use `IMeterFactory`. The meter name is a const on the module class and must be registered in `src/Host/Program.cs`, otherwise its metrics never leave the process.
- No personal data in logs, metrics or traces. Players are nicknames only.

### Packages

- Versions are managed centrally and pinned exactly in `Directory.Packages.props`.
- Each project has a `packages.lock.json`. Run `dotnet restore` after a package change and commit the lock files. CI restores in locked mode.

### Adding a module

1. A project `GleisGrain.<Module>` and `GleisGrain.<Module>.Contracts` under `src/Features/<Module>/`, a test project under `test/Features/<Module>/`, all added to `gleis-grain.slnx`.
2. A `<Module>Module` class with `Add<Module>Module` and `Map<Module>Endpoints`, plus `StorageName` and `MeterName` consts.
3. In `src/Host/Program.cs`: one `Add...Module` call, one `Map...Endpoints` call, its storage name and its meter name.
4. A database for it in `src/AppHost/Program.cs`, its path filter in `.github/workflows/ci.yml`, and an entry in `ModuleBoundaryTests`.

## Tests

- **Domain unit tests:** plain xunit, no silo. Most tests live here.
- **Grain integration tests:** an in-process `TestCluster` with in-memory storage and reminders. Other modules' grains are replaced by small fakes in the test project. No real time: use `FakeTimeProvider`.
- **Postgres integration test:** Testcontainers against the real Orleans schema scripts in `db/`. Only for what the in-memory silo cannot prove.
- **Frontend:** Vitest for the pure state logic, `npm run type-check` and `npm run build`.
- Name tests `Method_Condition_Result`. Linz AG is never called from a test: use the recorded replay data.

## Frontend

The Vue app in `src/Frontend` talks to the host only through the endpoints in [docs/api.md](docs/api.md). If you change an endpoint or an event payload, change that document and the matching `.http` file in `src/Host/http` in the same pull request. Keep state logic in plain TypeScript modules without Vue or Leaflet imports, so it can be unit tested.

## Releases

Releases are cut by pushing a tag on `main`. The release workflow checks the tag, builds and tests with that version and creates a GitHub release with generated notes.

- `1.2.3` is a release.
- `1.2.3-rc.1` is a release candidate and is marked as pre-release.

```
git tag 1.2.3-rc.1
git push origin 1.2.3-rc.1
```

## License

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE) of this project.
