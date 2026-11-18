# gleis-grain

Showcase for Microsoft Orleans built on public transport data from Linz AG. Stops, trips and service notices are virtual actors, kept up to date with reminders and timers. It is the demo for [a talk about the actor paradigm at the .NET Stammtisch Linz](https://www.meetup.com/net-stammtisch-linz/events/317130107/).

## Data source

Linz AG runs an EFA server (XML over HTTP). The departure monitor returns the departures of a stop with their delay, and every response carries the current service notices. Terms of use and rate limits are not checked yet, so the polling is polite and cached. Recorded responses can be replayed (`Ingestion:Mode`), so the demo and the tests do not depend on the server.

## Run it

In the devcontainer: `dotnet run --project src/AppHost`, or `--launch-profile replay` for recorded departures. The Aspire dashboard on http://localhost:15000 links everything: the app on http://localhost:5080 (three host replicas, one Orleans silo each), the Vue frontend, the Orleans Dashboard, Scalar and pgAdmin.

- **Audience:** the `simulator` resource plays about 80 virtual people through the public API. Click start on it in the dashboard, then steer the crowd (for example 50 people on Hauptbahnhof for a hot grain) with `src/Simulator/http/simulator.http` or the panel on the projector page.
- **Versioning demo:** the AppHost also knows a second build of the host with grain interface version 2. Start its replicas in the dashboard, then stop the v1 replicas: the stop grains move over while the app keeps running.

Details and troubleshooting: [CONTRIBUTING.md](CONTRIBUTING.md#running-the-demo).

## Bounded contexts

| Context   | Aggregate | What it owns                                                               |
|-----------|-----------|----------------------------------------------------------------------------|
| Stops     | Stop      | The curated stops, a stop's departure board, who watches it                 |
| Trips     | Trip      | A trip with its delay state                                                |
| Alerts    | Alert     | Service notices and disruptions                                            |
| Ingestion | Source    | The EFA XML client, parsing, rate limiting, live or replayed responses     |
| Game      | Player    | Nicknames, the chosen stop, delay bets and the leaderboard                 |
| Cluster   | Hub       | Silo overview, stats, the chaos button, the live event stream (SSE)        |

They call each other only through their Contracts projects. Live updates reach the browser through server-sent events on `GET /api/cluster/events`; see [docs/api.md](docs/api.md) and [ARCHITECTURE.md](ARCHITECTURE.md).

## Layout

A modular monolith: one deployable (`src/Host`) with all bounded contexts as modules; several replicas of it form one Orleans cluster.

```
src/Host/            the one deployable, composition root
src/Features/<Module>/  GleisGrain.<Module> and GleisGrain.<Module>.Contracts
src/Common/          shared HTTP, Orleans and OpenTelemetry setup
src/AppHost/         Aspire host: Postgres, host replicas, gateway, frontend, simulator
src/Simulator/       plays the audience through the HTTP API
src/Frontend/        Vue 3 + Vite app (Leaflet map)
db/                  Orleans SQL schema
test/                mirrors src
docs/                API, requirements and the talk (slides, diagrams)
```

## More

- [CONTRIBUTING.md](CONTRIBUTING.md): setup, workflow, commit messages, adding a module.
- [AGENTS.md](AGENTS.md): the rules and pitfalls for AI coding agents (and a good checklist for humans).
- [ARCHITECTURE.md](ARCHITECTURE.md): the architecture diagram. [docs/api.md](docs/api.md): endpoints and events. [docs/requirements.md](docs/requirements.md): features and demo script.

## License

[MIT](LICENSE).
