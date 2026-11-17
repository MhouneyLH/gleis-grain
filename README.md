# gleis-grain

Showcase for Microsoft Orleans built on public transport data from Linz AG. Stops, trips and service notices are virtual actors, kept up to date with reminders and timers.

The project is the demo for a talk about the actor paradigm at the .NET Stammtisch Linz.

> Work in progress. The six modules, the Vue frontend and the infrastructure (Postgres schema, three host replicas, CI) are built side by side, so single features may still be stubs. The HTTP contract between host and frontend is in [docs/api.md](docs/api.md).

## Data source

Linz AG runs an EFA server. Requests go to `https://www.linzag.at/static/XML_TRIP_REQUEST2` and the answer is XML. The departure monitor request of the same server works and returns the departures of a stop with their delay. Every response also carries the current service notices (stop relocations, detours, extra services), which the Alerts context uses.

Not checked yet: terms of use and rate limits. Live calls are the plan, so keep the polling polite. Recorded responses can be replayed (`Ingestion:Mode`), so the demo and the tests do not depend on the server.

## Getting Started

Everything runs in the devcontainer (.NET 10, Node 22, Docker access), so you only need VS Code with the Dev Containers extension and Docker on the host.

1. Open the repo in VS Code and choose "Reopen in Container".
2. Start the AppHost: `dotnet run --project src/AppHost`
3. The Aspire dashboard is on http://localhost:15000. It lists every resource and its links:
   - **Postgres and pgAdmin.** The first start loads the Orleans schema and creates one database per context plus `orleans` (see [db/](db/orleans/README.md)). Postgres runs these scripts only on an empty data volume. After a schema change reset it with `docker volume rm gleis-grain-postgres`, which also deletes all data.
   - **gleis-grain**, the host, in three replicas. Each replica is one Orleans silo with its own silo and gateway port that Aspire assigns. http://localhost:5080 load balances over the replicas:
     - `/dashboard` is the Orleans Dashboard: silos, grains, activations and request rates. It is switched on with `Orleans:EnableDashboard` in `src/Host/appsettings.json` and off by default in code, because it exposes internals of the cluster.
     - `/scalar` has the API docs, `/api/...` is the API.
   - **frontend**, the Vite dev server of the Vue app in `src/Frontend`, with hot reload. Its link is shown on the dashboard as "Frontend". It proxies `/api` to the host.
4. The AppHost turns the chaos button on (`Cluster:EnableChaos`) and starts the host with `Ingestion:Mode=Live`. For recorded data: `dotnet run --project src/AppHost -- Ingestion:Mode=Replay`.

To see the cluster, open pgAdmin from the dashboard and run `SELECT * FROM orleansmembershiptable;`. Every AppHost start is a new cluster id, so filter on the newest `deploymentid`.

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
  Host/               the one deployable: composition root, calls every module, serves the built frontend
  Common/             options, HTTP, OpenTelemetry and Orleans setup shared by all modules
  Common.Abstractions/ tiny shared base for strongly typed ids, safe to reference from Contracts
  Features/<Module>/
    GleisGrain.<Module>/            the module: Endpoints/, Domain/, options, metrics, logs, errors
    GleisGrain.<Module>.Contracts/  grain interfaces, snapshots and the module's ids
  Frontend/           Vue 3 + Vite app (Leaflet map, phone and projector views)
db/                   SQL: the Orleans schema scripts and init.sh that loads them
test/                 mirrors src, plus test/Modules (module boundaries) and test/Infrastructure (Postgres)
docs/                 requirements, API, architecture diagram and talk notes
```

The domain class (for example `Stop`) is plain C# and knows nothing about Orleans. The grain is a thin wrapper that loads the state, calls the domain class and writes the state back. API and silo run in the same process, so endpoints call `IGrainFactory` directly. Modules talk to each other through grain calls on their Contracts interfaces. Cluster membership and reminders live in the shared `orleans` database, grain state in each module's own database.

## More

- [CONTRIBUTING.md](CONTRIBUTING.md): build and test commands, workflow, commit messages, the rules that keep the modules apart, releases.
- [docs/api.md](docs/api.md): the HTTP endpoints and live events.
- [docs/architecture.md](docs/architecture.md): the architecture diagram.
- [docs/requirements.md](docs/requirements.md): features, demo script and open questions.
- Run all tests with `dotnet test --solution gleis-grain.slnx`. The Postgres test needs Docker and skips without it.

## License

[MIT](LICENSE).
