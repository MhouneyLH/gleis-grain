# gleis-grain

Showcase for Microsoft Orleans built on public transport data from Linz AG. Stops, trips and service notices are virtual actors, kept up to date with reminders and timers.

The project is the demo for [a talk about the actor paradigm at the .NET Stammtisch Linz](https://www.meetup.com/net-stammtisch-linz/events/317130107/).

## Data source

Linz AG runs an EFA server. Requests go to `https://www.linzag.at/static/XML_TRIP_REQUEST2` and the answer is XML. The departure monitor request of the same server works and returns the departures of a stop with their delay. Every response also carries the current service notices (stop relocations, detours, extra services), which the Alerts context uses.

Not checked yet: terms of use and rate limits. Live calls are the plan, so keep the polling polite. Recorded responses can be replayed (`Ingestion:Mode`), so the demo and the tests do not depend on the server.

## Run it

In the devcontainer: `dotnet run --project src/AppHost`. The Aspire dashboard on http://localhost:15000 links everything: the app on http://localhost:5080 (three host replicas, one Orleans silo each), the Orleans Dashboard, Scalar, the Vue frontend and pgAdmin. `--launch-profile replay` serves recorded departures instead of asking Linz AG. Setup and what the AppHost starts: [CONTRIBUTING.md](CONTRIBUTING.md#running-the-demo).

The audience cannot reach localhost, so the AppHost has a `simulator` resource that is not started automatically: click start on it in the Aspire dashboard when the audience should join. About 80 virtual people (`Sim-001`...) then join over 40 seconds through the public API, gather at Hauptbahnhof and move between stops. Tune it with `Simulator:People`, `Simulator:RampUpSeconds`, `Simulator:HotStopShare` and `Simulator:Seed`, for example `dotnet run --project src/AppHost -- Simulator:People=200`. Steer the crowd during the talk, for example 50 people on Hauptbahnhof for a hot grain: `src/Simulator/http/simulator.http` (control API on http://localhost:5090, see [docs/api.md](docs/api.md#simulator-control-api)) or the panel on the projector page.

Grain versioning demo: the AppHost starts `gleis-grain-v1` (three silos) and builds a second copy of the host with the grain interface version 2, `gleis-grain-v2` (three silos), which waits for you. Start its replicas in the Aspire dashboard (silos named `v2-...` join the cluster next to the `v1-...` ones), then stop the `gleis-grain-v1` replicas: the stop grains move to the v2 silos while the app keeps running. A reverse proxy named `gleis-grain` keeps http://localhost:5080 stable during the rollout.

## Bounded contexts

| Context   | Aggregate | What it owns                                                               |
|-----------|-----------|----------------------------------------------------------------------------|
| Stops     | Stop      | The curated stops, a stop's departure board refreshed by a reminder, who watches it |
| Trips     | Trip      | A trip with its delay state                                                |
| Alerts    | Alert     | Service notices and disruptions                                            |
| Ingestion | Source    | The EFA XML client, parsing, rate limiting, live or replayed responses     |
| Game      | Player    | Nicknames, the chosen stop, delay bets and the leaderboard                 |
| Cluster   | Hub       | Silo and activation overview, stats, the chaos button, the live event stream |

Calls between them go through the Contracts projects: Stops, Alerts and Trips fetch through Ingestion, Trips read from Stops, Game bets on trips and stops, and the modules publish what happens to the Cluster hub.

## Live updates

The browser gets changes through server-sent events on `GET /api/cluster/events`. Grains publish their events (stop activated, watchers, bets, leaderboard) to an observer hub grain in the Cluster module. Each silo subscribes one Orleans grain observer to the hub, and the subscription expires unless it is renewed, so a dead silo drops out by itself. An event raised on any silo therefore reaches a browser that is connected to any replica. The events are listed in [docs/api.md](docs/api.md).

## Layout

A modular monolith: one deployable (`src/Host`) that contains all bounded contexts as modules. Several replicas of it form one Orleans cluster of identical silos. Splitting a module into its own deployable later needs only a small host that calls its `Add...Module` and `Map...Endpoints`.

```
src/
  AppHost/            Aspire host, starts Postgres, three Host replicas and the Vite dev server
  Simulator/          console app that plays the audience through the public HTTP API, started from the Aspire dashboard
  Host/               the one deployable: composition root, calls every module, serves the built frontend
  Common/             options, HTTP, OpenTelemetry and Orleans setup shared by all modules
  Common.Abstractions/ tiny shared base for strongly typed ids, safe to reference from Contracts
  Features/<Module>/
    GleisGrain.<Module>/            the module: Endpoints/, Domain/, options, metrics, logs, errors
    GleisGrain.<Module>.Contracts/  grain interfaces, snapshots and the module's ids
  Frontend/           Vue 3 + Vite app (Leaflet map, phone and projector views)
db/                   SQL: the Orleans schema scripts and init.sh that loads them
test/                 mirrors src, plus test/Modules (module boundaries) and test/Infrastructure (Postgres)
docs/                 requirements, API and talk notes (the architecture diagram is ARCHITECTURE.md in the root)
```

The domain class (for example `Stop`) is plain C# and knows nothing about Orleans. The grain is a thin wrapper that loads the state, calls the domain class and writes the state back. API and silo run in the same process, so endpoints call `IGrainFactory` directly. Modules talk to each other through grain calls on their Contracts interfaces. Cluster membership and reminders live in the shared `orleans` database, grain state in each module's own database.

## More

- [CONTRIBUTING.md](CONTRIBUTING.md): build and test commands, workflow, commit messages, the rules that keep the modules apart, releases.
- [docs/api.md](docs/api.md): the HTTP endpoints and live events.
- [ARCHITECTURE.md](ARCHITECTURE.md): the architecture diagram and how the parts fit.
- [AGENTS.md](AGENTS.md): the important rules and pitfalls for AI coding agents.
- [docs/requirements.md](docs/requirements.md): features, demo script and open questions.
- Run all tests with `dotnet test --solution gleis-grain.slnx`. The Postgres test needs Docker and skips without it.

## License

[MIT](LICENSE).
