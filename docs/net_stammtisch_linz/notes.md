# .NET Stammtisch Linz

- additional: how Orleans works under the hood (diagrams): [how-orleans-works.md](how-orleans-works.md)

## Why this project
- The domain has to be easy to picture and the speaker has to understand it deeply. Linz AG public transport fits: everybody has waited at a stop in Linz.
- It started from an Uber-style dispatch plan. The Orleans ideas stay the same, only the domain changed.
- Other ideas that were considered: a solar fleet twin with an AI agent per household, a live audience app (QR code, votes, quiz), "watcher" agents for NuGet or CVE feeds, a multiplayer game with AI bots.

### Original idea -> gleis-grain
| Original (dispatch) | gleis-grain | Why it works the same |
|---|---|---|
| `GeoCellGrain` (never search all drivers) | `StopGrain` (ask one stop, not the whole network) | Partition by key, the spatial problem becomes a key lookup |
| `DriverGrain` | `TripGrain` | Moving thing with state that gets stale |
| `RideGrain` state machine | `AlertGrain`, delay state in `TripGrain` | State with a lifecycle |
| Airport cell at rush hour | Linz Hbf | Hot grain |
| Timer: driver offline after 30 s | Timer: refresh board while somebody watches | In memory, frequent, fine to lose |
| Reminder: ride without driver after 2 min | Reminder: refresh every stop each minute | Durable, survives a silo crash |
| Simulated cars on a map | Live Linz AG XML (EFA) | Real data instead of a simulator |

### Contexts in this repo
Stops, Trips, Alerts, Ingestion (XML client and rate limiter). See the README and [architecture.md](../architecture.md).
Data: Linz AG EFA server, XML over HTTP. Terms of use and rate limits: not checked yet.

## Agenda (45 min)

### 0-4 Why actors
- One sentence on Fronius: devices are actors. A stop or a trip is the same idea.
- Virtual actors: no lifecycle management, location transparency, single-threaded execution.
- In the repo: one grain per aggregate (`StopGrain`, `TripGrain`, `AlertGrain`, `SourceGrain`). Endpoints call `grains.GetGrain<IStopGrain>(id)`, no create or lifecycle code (`Endpoints/GetStop.cs`).
- A stop nobody asks about costs nothing, show activation and deactivation in the dashboard: [TBD]

### 4-10 Basics: grains and state
- Show a grain with `IPersistentState<T>`. In the repo: `StopGrain` with `StopState`, named storage per context, Postgres behind it.
- Domain class (`Stop`) is plain C#, the grain is a thin wrapper that loads, calls and saves. Good for tests without a silo.
- Practice: do not persist every refresh. Keep the data in memory and write it on a timer (write-behind). Losing 10 seconds of departures is fine: [TBD]
- Mark fire-and-forget updates with `[OneWay]`: [TBD]
- Departure board as state, parsed from the XML: [TBD]

### 10-15 Timers vs reminders
- Timer: in memory, frequent, fine to lose. Here: refresh a board every 10-15 s while somebody watches it: [TBD]
- Reminder: durable, survives silo crashes. Here: refresh every stop each minute: [TBD]
- Rule of thumb: if it must happen even after a crash, use a reminder.
- Wired already: reminder service on Postgres (shared `orleans` database), `ReminderPeriod` option per context.

### 15-21 Concurrency and hot spots
- Deadlock trap: A calls B while B calls A. Show the timeout, then fix it with `[Reentrant]`, `[AlwaysInterleave]` or a different flow (Trip <-> Stop): [TBD]
- Mark queries `[ReadOnly]` so they can interleave: [TBD]
- Hot grain: Linz Hbf. Options: `[StatelessWorker]` for read-heavy lookups, smaller keys, split writes from reads: [TBD]
- Rate limiter grain in Ingestion, so hundreds of reminders do not hammer the Linz AG server: [TBD]

### 21-27 Cluster in action
- Run 3 silos (Aspire makes this easy and gives a dashboard). Kill one in the middle of work: grains come back on another silo, the reminder still fires: [TBD]
- Membership in the shared `orleans` database (Postgres here, Redis or Azure Table are the other usual choices). Demo query on the big screen: `SELECT * FROM orleansmembershiptable;` in pgAdmin. The audience sees the dead silo flip.
- Cover: membership, grain directory, placement. See [how-orleans-works.md](how-orleans-works.md).
- Placement: `[PreferLocalPlacement]` and others. Default since Orleans 9.2 is resource-optimized, before that random: [TBD demo]
- Modular monolith: one host contains all modules (API and silo in one process). Replicas of it form one cluster of identical silos. Splitting into one silo type per module is possible later.
- Orleans SQL schema loaded into Postgres: [TBD] (needed before anything runs)

### 27-33 Versioning (most talks skip this)
- Serialization contracts: `[GenerateSerializer]`, stable `[Id(n)]` per field, `[Alias("...")]` so renaming a type or namespace does not break anything. Only add fields with new ids, never reuse or retype an id. Already on every state, snapshot, id and grain interface.
- Persisted state: `SchemaVersion` field (there) and migrate in `OnActivateAsync` (missing): [TBD]
- Grain interface versioning: `[Version(2)]`, compatibility strategies (e.g. backward compatible), version selectors (latest, minimum). v1 and v2 silos can coexist during a rolling update: [TBD]
- Demo: rolling deployment of v2 with an extra field (for example platform on `StopGrain`) while load is running, no errors: [TBD]

### 33-37 Kubernetes and load test
- `Microsoft.Orleans.Hosting.Kubernetes` with `UseKubernetesHosting()`: [TBD]
- Pod labels `orleans/serviceId` and `orleans/clusterId`, `POD_NAME`, `POD_NAMESPACE`, `POD_IP` via downward API, RBAC so silos can see pods, readiness and liveness probes, a long enough `terminationGracePeriodSeconds`: [TBD]
- Load test (pre-recorded): NBomber, simulate thousands of stops and watchers on replayed XML, show throughput scaling from 1 to 3 pods. Never load test the real Linz AG server: [TBD]

### 37-45 Takeaways and discussion
Discussion matters more than finishing the demo. Questions for the room:
- Where would grains hurt you? When is a plain cache and a cron job the better answer?
- One cluster for all contexts or one per context?
- Reminders or a job scheduler such as Quartz or Hangfire?
- Grain state is a blob (`SELECT` cannot filter it). Accept that or project into your own tables (read model, CQRS)?
- How do you roll out a breaking state change safely?
- Is polling a third party API from many grains polite? Where does the rate limit live?
- Where would an LLM agent per grain make sense, and where is it a gimmick?

## Postgres for clustering, state and reminders
- Packages: `Microsoft.Orleans.Clustering.AdoNet`, `Persistence.AdoNet`, `Reminders.AdoNet`, `Npgsql`. In `OrleansExtensions.cs`.
- Orleans does not create tables. Run the `Main` script first, then clustering, persistence, reminders. Scripts are in the Orleans repo under `src/AdoNet`, take them from the tag that matches the version (10.4.0). [TBD]
- State is stored as a blob in one generic table, not as columns. Old blobs stay across deployments, so the `SchemaVersion` migration matters even more.
- Aspire shortcut: the community package `Shiny.Aspire.Orleans` runs the schema scripts for you. The manual way teaches more.

## Extras if there is room
- Aspire for the local dev loop: one AppHost starts Postgres, pgAdmin and every silo. Done. A great intro demo.
- OpenTelemetry: Orleans meter and activity sources are exported (`ObservabilityExtensions.cs`). A distributed trace Stop -> Ingestion in the Aspire dashboard needs those calls: [TBD]
- Event sourcing with `JournaledGrain`: audit trail of every change: [TBD]
- Streams: push updates to a browser: [TBD]
- Testing: domain classes are unit tested. In-process `TestCluster` for grains: [TBD]
- AI angle: a small agent that explains a delay from the notices and the trip history, as a fun closer: [TBD]
- Static departure page later: [TBD]

## Practical
- Pre-record the load test and the rolling update as backups. Live Kubernetes demos at meetups fail on Wi-Fi more often than on code.
- Done in the repo: Aspire AppHost, OpenTelemetry wiring, strongly typed ids as grain keys, per-context databases, CI per context, releases from tags.
