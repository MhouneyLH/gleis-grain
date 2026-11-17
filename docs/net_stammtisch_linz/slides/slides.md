# Next Stop: Virtual Actors

### Microsoft Orleans with Live Linz Data

.NET Stammtisch Linz

Lucas Hünniger & Raffael Rehberger

<!-- TODO IMAGE: title background, for example a Linz tram at the Hauptbahnhof (public/images/title.jpg) -->

---

## Who we are

<div class="speakers">
  <div class="speaker">
    <div class="photo"><!-- TODO IMAGE: replace with <img src="/images/lucas.jpg" alt="Lucas Hünniger"> (square, at least 800 x 800 px) -->TODO IMAGE<br>lucas.jpg</div>
    <h3>Lucas Hünniger</h3>
    <ul>
      <li>Backend Engineer by day, Hobby Platform Engineer at night</li>
      <li>Orleans is part of daily work</li>
      <li>Cloud Native, OpenTelemetry, .NET</li>
    </ul>
  </div>
  <div class="speaker">
    <div class="photo"><!-- TODO IMAGE: replace with <img src="/images/speaker2.jpg" alt="Second speaker"> (square, at least 800 x 800 px) -->TODO IMAGE<br>speaker2.jpg</div>
    <h3>Raffael Rehberger</h3>
    <ul>
      <li>TODO: role in one line</li>
      <li>TODO: link to the topic</li>
      <li>TODO: interests</li>
    </ul>
  </div>
</div>

---

## Agenda
 
- Why actors?
- Grains and state
- Timers vs reminders
- Concurrency and hot spots
- Cluster in action
- Versioning
- Kubernetes and load test
- Take this home

---

## Why actors?

- A **stop** is an actor
- One identity, its own state, one request at a time
- Nobody searches "all stops", you ask one stop

<!-- TODO IMAGE: diagram "stop = actor": one identity, its own state, one request at a time -->

Note:
Everybody has waited at a stop in Linz, so the domain is easy to picture. It started as an Uber-style dispatch plan, a stop replaces the geo cell: partition by key, the spatial problem becomes a key lookup.

---

## Virtual actors in one slide

- A **grain** is an object with an identity and its own state
- You never create or destroy it, you just call it
- Orleans activates it on the first call, forgets it when idle
- It *always exists logically*, like virtual memory

```csharp
var stop = grains.GetGrain<IStopGrain>(stopId);
var board = await stop.GetBoardAsync();
```

Note:
Careful: the snippet is illustrative, check the real method names in `Endpoints/GetStop.cs` before showing code. Office analogy from how-orleans-works.md: grain = clerk for one stop, silo = building, cluster = all buildings, activation = clerk at the desk. A typo in an id silently gets its own empty grain, so validate ids at the edge.

--

## Grains, silos, cluster

| Orleans | Office |
|---|---|
| Grain | Clerk for exactly one stop |
| Silo | An office building (a .NET process) |
| Cluster | All buildings |
| Activation | Clerk at the desk right now |

<!-- TODO IMAGE: diagram of the cluster with silos and grains (Mermaid source in how-orleans-works.md, "The big picture") -->

Note:
Vertical slide, can be skipped. Location transparency: you never say which silo.

---

## Demo 1: stops light up

- Open the map
- Stops glow while their grain is active
- They fade when it is deactivated

<div class="placeholder">QR CODE / URL placeholder: public/images/qr.svg and https://TODO-audience-url</div>

Note:
Demo step 1 (virtual actors, feature F3). Audience action: join by QR code with a nickname only, open a stop. Watch the dots light up and fade. Fallback: replay mode. Check that the map shows the activation view in the current build before the talk.

---

## A grain with persistent state

- `IPersistentState<StopState>`, named storage, Postgres behind it
- State is a blob, one generic table
- Domain class `Stop`: plain C#, no Orleans
- Grain: thin wrapper, load, call, save

```csharp
public class StopGrain(
    [PersistentState("stop", StopsOptions.StorageName)]
    IPersistentState<StopState> state, ...) : Grain, IStopGrain
```

Note:
In the repo: `StopGrain` with `StopState` (src/Features/Stops). The domain class can be unit tested without a silo. Restart a silo in the demo: the state is still there (demo step 2). Write-behind (do not persist every refresh) and `[OneWay]` are talking points only, not implemented.

---

## What to persist, what to let go

- Persist: what must survive a crash
- Let go: what the next refresh rebuilds anyway
- Losing 10 seconds of departures is fine
- Bets must survive, so they are persisted

<!-- TODO IMAGE: two columns "persist" vs "let go" with examples -->

Note:
Lessons learned from daily work. In the repo the stop board is refreshed from Linz AG, the player bets are state. Write-behind is a talking point, not built (planned).

---

## Timers vs reminders

| | Timer | Reminder |
|---|---|---|
| Lives | In memory | Durable (Postgres) |
| Survives silo crash | No | Yes |
| Use for | Frequent, fine to lose | Must happen anyway |
| In gleis-grain | Faster refresh while watched | Refresh every minute, resolve bets |

Note:
Rule of thumb: if it must happen even after a crash, use a reminder. Real code: `RegisterGrainTimer` and `RegisterOrUpdateReminder` in `StopGrain`, reminders in `PlayerGrain` (bet resolution) and `AlertBoardGrain`. Reminder period is an option per context. Exact timer period: check the options before quoting a number.

---

## Demo 2: place a bet

- Pick a stop
- Guess the delay of the next departure (0 to 15 minutes)
- Exact guess: 3 points, off by one: 1 point
- A reminder resolves the bet

<div class="placeholder">TODO IMAGE: phone screenshot of the bet screen</div>

Note:
Demo step 3 (F1, F5). Audience action: bet on a delay. Show the timer while someone watches, the reminder otherwise. Bets survive a silo restart because the state is persisted and the reminder is durable. Live leaderboard by nickname.

---

## One request at a time

- Every grain has a mailbox, one message at a time (a turn)
- No locks inside the grain
- The price: a slow call blocks the next ones
- Deadlock: A waits for B, B waits for A, timeout

<!-- TODO IMAGE: mailbox diagram and the Trip <-> Stop deadlock sequence (Mermaid in how-orleans-works.md) -->

Note:
Planned: the Trip <-> Stop deadlock demo is not built, treat it as a whiteboard story. Options: `[ReadOnly]` for queries, `[AlwaysInterleave]` for small independent methods, `AllowCallChainReentrancy()` for a known cycle, `[Reentrant]` only for grains with almost no state. Best fix: break the cycle by design. Reentrancy only changes scheduling inside one activation, it never creates a second one.

---

## The hot grain: Hauptbahnhof

- Everybody picks the same stop
- One activation, one mailbox, one thread
- Fix: a `[StatelessWorker]` board cache
- Many local copies, read from a snapshot

```csharp
[StatelessWorker, GrainType("stop-board-cache")]
public class StopBoardCacheGrain : Grain, IStopBoardCacheGrain
```

<!-- TODO IMAGE: chart, watchers per stop with Hauptbahnhof as the tall bar -->

Note:
Demo step 4 (F4): everyone picks a stop, Hauptbahnhof gets hot, then show the fix. `StopBoardCacheGrain` is real (src/Features/Stops). `[StatelessWorker]` is the one case where one id has several activations. Do not quote throughput numbers, none were measured.

---

## The rate limiter grain

- Many reminders, one Linz AG server
- All requests go through one `SourceGrain`
- One shared rate limit, results cached in the grains
- Non-reentrant on purpose: it counts in order

<!-- TODO IMAGE: arrows from many StopGrains into one SourceGrain, then to the EFA server -->

Note:
Module Ingestion: EFA XML client, parser, rate limiting, live or replayed responses (`Ingestion:Mode`). Linz AG terms of use and rate limits are still unchecked (open question), keep polling polite. Never load test the real server.

---

## Cluster: membership

- Silos watch each other and vote a dead silo out
- Membership lives in the shared `orleans` Postgres database
- Dead silo: its grains are activated again on another silo
- Reminders still fire

```sql
SELECT * FROM orleansmembershiptable;
```

Note:
In pgAdmin on the big screen, the audience sees the dead silo flip. Details in how-orleans-works.md (membership, when a silo dies). Postgres is one choice, Redis or Azure Table are other usual ones.

---

## Cluster: directory and placement

- Directory: distributed, in memory, split over all silos by hash ranges
- Normally one activation per grain
- Placement: where does a new activation go?
- Default since Orleans 9.2: resource optimized (before: random)

<!-- TODO IMAGE: hash ring diagram (Mermaid in how-orleans-works.md, "Who knows where a grain lives?") -->

Note:
Placement demo with `[PreferLocalPlacement]` and others was not built, talking point only. Directory is not in Postgres, after a full restart it fills again as calls arrive.

---

## Modular monolith, 3 replicas

- One deployable, all bounded contexts as modules
- API and silo in one process
- 3 replicas form one cluster of identical silos
- Modules talk through Contracts projects only

| Stops | Trips | Alerts |
|---|---|---|
| Ingestion | Game | Cluster |

<!-- TODO IMAGE: docs/architecture.md diagram exported as SVG (Excalidraw: Insert > Mermaid to Excalidraw) -->

Note:
Splitting a module into its own deployable later needs only a small host that calls its `Add...Module` and `Map...Endpoints`. Aspire AppHost starts Postgres, three Host replicas, the Vite dev server and pgAdmin. Splitting into one silo type per module is possible, not done.

---

## Live updates: observers and SSE

- Grains publish events to one hub grain
- Every silo subscribes one grain observer
- Subscriptions expire unless renewed
- Each replica pushes events to browsers as SSE

<!-- TODO IMAGE: diagram grain -> hub -> observer per silo -> SSE -> browser -->

Note:
`GET /api/cluster/events`. An event raised on any silo reaches a browser connected to any replica. A dead silo drops out by itself because its subscription expires. Deliberately not Orleans streams.

---

## Demo 3: break the cluster

- Vote to kill a silo
- Map dots change colour, membership flips
- Grains come back on the other silos
- Bets still resolve

<div class="placeholder">TODO IMAGE: screenshot of the map with silo colours, before and after</div>

Note:
Demo step 5 (F6, F7). Audience action: vote to kill a silo. The chaos button kills the replica that receives the request, only when `Cluster:EnableChaos` is true (the AppHost sets it for the demo). Needs the 3 replicas. Rehearse it, and keep a screen recording as a backup.

---

## Versioning: serialization

- `[GenerateSerializer]` on every state and contract
- Stable `[Id(n)]` per field, add new ids only
- Never reuse or retype an id
- `[Alias("...")]` so renames do not break anything

```csharp
[GenerateSerializer, Alias("GleisGrain.Stops.Domain.StopState")]
internal sealed class StopState
{
    [Id(0)] public int SchemaVersion { get; set; } = 1;
    [Id(1)] public List<string> Watchers { get; set; } = [];
}
```

Note:
This is in the repo on state, snapshots, ids and grain interfaces. Snippet shortened from the real `StopState` (more fields with ids 2 and 3). State is a blob and old blobs stay across deployments, so this matters a lot.

---

## Versioning: rolling updates

- `SchemaVersion` field exists, migration in `OnActivateAsync` is missing
- `[Version(2)]` on grain interfaces
- Compatibility strategy and version selector
- v1 and v2 silos coexist during a rolling update

<!-- TODO IMAGE: timeline of a rolling update with v1 and v2 silos -->

Note:
PLANNED, NOT BUILT: no `[Version]` attribute in the repo, no migration code, no rolling update demo (optional demo step 6, extra field such as platform on `StopGrain`). Present as a talk topic or show a pre-recorded run. Discussion question: how do you roll out a breaking state change safely?

---

## Kubernetes and load test

- `UseKubernetesHosting()` from `Microsoft.Orleans.Hosting.Kubernetes`
- Pod labels `orleans/serviceId`, `orleans/clusterId`
- Downward API env vars, RBAC, probes, long grace period
- Load test with NBomber on replayed XML

Note:
PLANNED, NOT BUILT: no Kubernetes manifests and no load test exist in the repo. Talk about it only, no results, do not invent numbers. If a pre-recorded load test is made later, label it as recorded. First block to cut if time is short.

---

## Testing

- Domain classes: plain unit tests, no silo
- Grains: in-process `TestCluster`
- Storage: in-memory in tests
- Postgres: Testcontainers (skips without Docker)

```bash
dotnet test --solution gleis-grain.slnx
```

Note:
Tests mirror `src`, plus module boundary tests. Recorded XML responses make tests and CI independent from Linz AG.

---

## Observability

- OpenTelemetry: Orleans meter and activity sources exported
- Aspire dashboard: resources, logs, traces, metrics
- Orleans Dashboard: silos, grains, activations

<!-- TODO IMAGE: Aspire dashboard screenshot with a trace -->

Note:
Show the Aspire dashboard (http://localhost:15000) in the demo. A distributed trace Stop -> Ingestion needs those calls instrumented, verify what the dashboard shows before promising it.

---

## Lessons learned

- Persist what must survive, let go of the rest
- Keep domain code free of Orleans, the grain stays thin
- One id, one actor: validate ids at the edge
- Break call cycles by design before reaching for `[Reentrant]`
- Think about versioning before the first deployment

Note:
Tie back to daily Orleans work. Keep these personal and honest, only claim what you have seen in production.

---

## Discussion

- Where would grains hurt you? When is a cache and a cron job better?
- One cluster for all contexts or one per context?
- Reminders or a scheduler such as Quartz or Hangfire?
- Grain state is a blob. Accept it or project into read models (CQRS)?
- How do you roll out a breaking state change safely?

Note:
Ask two or three, let the room choose. More questions if there is time: is polling a third party API from many grains polite, and where does the rate limit live? Where would an LLM agent per grain make sense, and where is it a gimmick?

---

## Free play

- Join with your phone
- Pick a stop
- Bet on a delay
- Climb the leaderboard

<div class="placeholder">QR CODE / URL placeholder: public/images/qr.svg and https://TODO-audience-url</div>

Note:
Demo step 7 (F4, F5, F9). Nicknames only, 2 to 20 characters, unique ignoring case. Scale target is about 100 phones, one stop each. The stats panel shows cache hits versus calls to Linz AG, active grains, hottest stops.

---

## Takeaways

- A virtual actor is an identity, memory and activations come and go
- Timers for the cheap, reminders for the important
- Hot grains need a design, not just more silos
- Persist less, version early

Note:
Four sentences, then back to discussion.

---

## Thanks

- Code: https://github.com/MhouneyLH/gleis-grain
- Meetup: https://www.meetup.com/net-stammtisch-linz/events/317130107/
- Slides: https://TODO-slides-url
- Orleans docs: https://learn.microsoft.com/dotnet/orleans

<div class="placeholder">TODO IMAGE: QR code to the repo (public/images/qr-repo.svg)</div>

Note:
Replace the TODO links before the talk. Data from Linz AG (EFA server), map tiles from OpenStreetMap, credit both.
