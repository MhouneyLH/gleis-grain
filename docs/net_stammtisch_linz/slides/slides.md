# Next Stop: Virtual Actors

### Microsoft Orleans on the Linz tram network  

.NET Stammtisch Linz

Lucas Hünniger & Raffael Rehberger

Note:
- Speaker: Lucas

---

## Who we are

<div class="speakers">
  <div class="speaker">
    <img src="/images/lucas.png" alt="Lucas Hünniger">
    <h3>Lucas Hünniger</h3>
    <p>Backend Engineer by day, Hobby Pla   tform Engineer by night</p>
    <p>Orleans is part of daily work</p>
    <p>Cloud Native, OpenTelemetry, .NET</p>
  </div>
  <div class="speaker">
    <img src="/images/raffi.jpg" alt="Raffael Rehberger">
    <h3>Raffael Rehberger</h3>
    <p>TODO: role in one line</p>
    <p>TODO: link to the topic</p>
    <p>TODO: interests</p>
  </div>
</div>

Note:
- Speaker: Lucas | Raffi

---

## Agenda

- Project Intro
- What are actors? ... and why???
- Grains and state
- Cluster in action
- Timers vs reminders
- Concurrency and hot spots
- Versioning
- Kubernetes
- Testing
- Take this home

Note:
- Speaker: Lucas

Timings in minutes (start-end of the talk):
- Project Intro: 5 min (0-5)
- What are actors? ... and why???: 5 min (5-10)
- Grains and state: 5 min (10-15)
- Cluster in action: 5 min (15-20)
- Timers vs reminders: 3 min (20-23)
- Concurrency and hot spots: 5 min (23-28)
- Versioning: 5 min (28-33)
- Kubernetes -> bridge to loda tests & testing in general: 3 min (33-36)
- Testing: 3 min (36-39)
- Take this home: 2 min (39-41)
- Questions: 5 min (41-46)

---

## Project Intro: the domain

- Linz AG tram network, everybody knows a stop
- **Stop**, **trip**, **notice**, **audience** are actors
- Live departures from the Linz AG EFA server (XML)

<div class="placeholder">photo of a Linz tram at Hauptbahnhof (or a stop display)</div>
<!-- TODO IMAGE: photo of a Linz tram at Hauptbahnhof (or a stop display), name: tram_hbf.jpg -->

Note:
- Speaker: Lucas

Everybody has waited at a stop in Linz. Data comes from the Linz AG EFA server as XML (live, or recorded responses replayed with `Ingestion:Mode`). Linz AG terms of use and rate limits are not checked yet, keep polling polite.

---

## Project Intro: the map

- Stops are dots on a map
- Dot size: how many people watch the stop
- Shape and colour: which silo and which version runs the grain

<div class="placeholder">screenshot: projector page, map with stops as dots (silo shapes, size by watchers)</div>
<!-- TODO IMAGE: screenshot of the projector page `/projector`, map with stops as dots, name: map_projector.png. Map tiles are from OpenStreetMap: keep the attribution "© OpenStreetMap contributors" visible. -->

<p class="small">Map data © OpenStreetMap contributors</p>

Note:
- Speaker: Lucas

Shape = silo is the design for the demo, colour = grain version belongs to the versioning demo (PLANNED, not built yet, see versioning). Public OpenStreetMap tiles are fine for a demo, the attribution has to stay visible.

---

## Project Intro: what you will see

- The map on the projector
- Your phone picks a stop
- We break the cluster
- A rolling version update

<div class="placeholder">screenshot: Aspire dashboard with 3 gleis-grain replicas</div>
<!-- TODO IMAGE: screenshot of the Aspire dashboard with 3 gleis-grain replicas, name: aspire_replicas.png -->

Note:
- Speaker: Lucas | Raffi

---

## Demo 1: live map

- Scan the QR code, type a nickname
- Tap a stop
- Watch the dot light up

<div class="placeholder">QR CODE + URL of the audience page (/), same as the Free play slide</div>
<!-- TODO IMAGE: QR code for the audience page, name: qr.svg (also used on the Free play slide) -->

Note:
- Speaker: Lucas | Raffi

About 2 min. What to do (steps marked PLAN depend on the frontend being finished):
1. Before the talk: `dotnet run --project src/AppHost`. If the Wi-Fi is bad: `dotnet run --project src/AppHost --launch-profile replay` (recorded departures, no Linz AG calls).
2. Open the Aspire dashboard http://localhost:15000 and the frontend link from there. Say: "Three replicas, one Orleans silo each, one cluster."
3. Open the projector page `/projector` and make it full screen on the beamer. Say: "Every dot is a stop, every stop is a grain."
4. Show the QR code or URL for the audience page `/`. Say: "Scan, pick a nickname, tap a stop."
5. Audience types a nickname and taps a stop. Dots light up and shrink or grow with the number of watchers. Say: "Nobody created these actors, the first call activated them."
6. Point at the silo shapes. Say: "The shape tells you which silo hosts the grain." (PLAN: shape = silo is not built yet, confirm.)

If it fails: restart with `--launch-profile replay`; if the app itself is down, play the pre-recorded video (TODO: record it, keep it local on the laptop) and keep talking.

---

## What are actors? ... and why???

- how we would do with classic microservice -> netflix architecture horror
- A **stop** is an actor
- One identity, its own state, one request at a time
- Nobody searches "all stops", you ask one stop

<!-- TODO IMAGE: diagram "stop = actor": one identity, its own state, one request at a time -->

Note:
- Speaker: Raffi

Everybody has waited at a stop in Linz, so the domain is easy to picture. Partition by key: you ask one stop, not the whole network.

---

## Virtual actors in Orleans

- A **grain** is an object with an identity and its own state
- You never create or destroy it, you just call it
- Orleans activates it on the first call, forgets it when idle
- It *always exists logically*, like virtual memory

```csharp
var stop = grains.GetGrain<IStopGrain>(stopId);
var board = await stop.GetBoardAsync();
```

Note:
- Speaker: Raffi

Careful: the snippet is illustrative, check the real method names in `Endpoints/GetStop.cs` before showing code. Office analogy from how-orleans-works.md: grain = clerk for one stop, silo = building, cluster = all buildings, activation = clerk at the desk. A typo in an id silently gets its own empty grain, so validate ids at the edge.

---

## Example: StopGrain

```csharp
myexample
```

Note:
- Speaker: Lucas


---

## Grains, silos, cluster

| Orleans | Office |
|---|---|
| Grain | Clerk for exactly one stop |
| Silo | An office building (a .NET process) |
| Cluster | All buildings |
| Activation | Clerk at the desk right now |

<!-- TODO IMAGE: diagram of the cluster with silos and grains (Mermaid source in how-orleans-works.md, "The big picture") -->

Note:
- Speaker: Lucas

Vertical slide, can be skipped. Location transparency: you never say which silo.

---

## Cluster in Action

- placement strategies
- sequence diagram to show one full call from frontend to grain calls
    - what happens in orleans middleware -> communication protocol, binary serialization, grain directory 

Note:
- Speaker: Raffi

---

## Timers vs reminders
- persisting
- eventually consistent behavior of reminders


Note:
- Speaker: Raffi

---

## Concurrency and hot spots
- interleave & reentrancy
- stateless worker
- readonly 
- hot grain: Hauptbahnhof -> bad architectural decision or batching
    - sourcegrain -> grain chains block each other, one global grain; show at rate limiter

Note:
- Speaker: Lucas

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
- Speaker: Lucas

This is in the repo on state, snapshots, ids and grain interfaces. Snippet shortened from the real `StopState` (more fields with ids 2 and 3). State is a blob and old blobs stay across deployments, so this matters a lot.

---

## Versioning: rolling updates

- `SchemaVersion` field exists, migration in `OnActivateAsync` is missing
- `[Version(2)]` on grain interfaces
- Compatibility strategy and version selector
- v1 and v2 silos coexist during a rolling update

<div class="placeholder">timeline of a rolling update with v1 and v2 silos</div>
<!-- TODO IMAGE: timeline of a rolling update with v1 and v2 silos, name: rolling_update.svg -->

Note:
- Speaker: Lucas

PLANNED, NOT BUILT: no `[Version]` attribute in the repo, no migration code, no rolling update demo (optional demo step 6, extra field such as platform on `StopGrain`). Discussion question: how do you roll out a breaking state change safely?

---

## Versioning: how to read the map

- Colour = grain version: **red** v1, **blue** v2
- Shape = silo: square, circle, triangle
- Size = watchers

<div class="placeholder">screenshot: map legend, red and blue dots in square, circle and triangle shapes</div>
<!-- TODO IMAGE: screenshot of the map legend (red v1, blue v2; square, circle, triangle = silo), name: map_legend.png -->

Note:
- Speaker: Lucas

PLANNED, NOT BUILT: this encoding is the design for the versioning demo, another agent is building it. Confirm colours and shapes against the real frontend before the talk [CONFIRM].

---

## Demo: rolling update

- Red dots: everything runs v1
- Start v2 next to v1
- Stop v1 one by one
- Dots turn blue, the app keeps answering

<div class="placeholder">screenshot: Aspire dashboard with gleis-grain-v1 and gleis-grain-v2 side by side</div>
<!-- TODO IMAGE: screenshot of the Aspire dashboard with gleis-grain-v1 and gleis-grain-v2 resources, name: aspire_v1_v2.png [CONFIRM resource names] -->

Note:
- Speaker: Lucas | Raffi

PLANNED, NOT BUILT: the whole demo below is a plan, nothing of it exists in the repo yet. Resource names, buttons and colours are marked [CONFIRM] until the other agent has finished. Slot: 5 min.

Before the talk (not on stage):
- AppHost runs 3 v1 replicas, resource `gleis-grain-v1` [CONFIRM], and a v2 resource `gleis-grain-v2` [CONFIRM] with 3 replicas that is NOT started.
- Ask the audience to keep several stops open, so many grains are active (otherwise nothing to see).

On stage:
1. (0:30) Projector page: all dots red, three shapes. Say: "Everything runs version 1, three silos."
2. (0:30) Aspire dashboard http://localhost:15000: click start on `gleis-grain-v2` [CONFIRM]. Say: "Now the new version comes up next to the old one, nobody is switched off."
3. (0:45) Show the silo list: 6 silos, v1 and v2 side by side (Orleans Dashboard or the map legend [CONFIRM where]). Say: "One cluster, two versions."
4. (1:15) Explain Orleans interface versioning: `[Version(2)]` on the interface, BackwardCompatible strategy, LatestVersion selector. Say: "New activations go to v2, v1 callers still work, the cluster does not care."
5. (1:30) Click stop on the v1 replicas one by one [CONFIRM: stop button per replica]. Watch grains move to other silos and dots turn blue, while the app keeps answering (tap a stop on the phone to prove it). Say: "A rolling update without a maintenance window."
6. (0:30) Take-away: "Interface versions plus a compatible state format make deployments boring. Think about it before the first deployment." Back to Take this home.

If it fails:
- Play the pre-recorded video (TODO: record it, label it as recorded).
- If the dots stay red because nobody watches: open some stops yourself on the phone or laptop. Only grains that are activated move, idle ones are activated on v2 when called next.
- If v2 does not start: skip steps 2 to 3, explain step 4 with the slide and stop at "what would happen".

---

## Demo Time

Note:
- Speaker: Lucas | Raffi

<!-- HIIIIIIIIIIIER STARTEN DIE GENERIERTEN SLIDES -->

---

## A grain with persistent state

- `IPersistentState<StopState>`, named storage, Postgres behind it
- State is a blob, one generic table
- Domain class `Stop`: plain C#, no Orleans
- Grain: thin wrapper, load, call, save

```csharp
public sealed class StopGrain(
    [PersistentState("stop", StopsOptions.StorageName)]
    IPersistentState<StopState> state, ...) : Grain, IStopGrain
```

Note:
- Speaker: Lucas | Raffi

In the repo: `StopGrain` with `StopState` (src/Features/Stops). The domain class can be unit tested without a silo. Restart a silo in the demo: the state is still there (demo step 2). Write-behind (do not persist every refresh) and `[OneWay]` are talking points only, not implemented.

---

## What to persist, what to let go

- Persist: what must survive a crash
- Let go: what the next refresh rebuilds anyway
- Losing 10 seconds of departures is fine
- Bets must survive, so they are persisted

<!-- TODO IMAGE: two columns "persist" vs "let go" with examples -->

Note:
- Speaker: Lucas | Raffi

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
- Speaker: Lucas | Raffi

Rule of thumb: if it must happen even after a crash, use a reminder. Real code: `RegisterGrainTimer` and `RegisterOrUpdateReminder` in `StopGrain`, reminders in `PlayerGrain` (bet resolution) and `AlertBoardGrain`. Reminder period is an option per context. Exact timer period: check the options before quoting a number.

---

## Demo 2: place a bet

- Pick a stop
- Guess the delay of the next departure (0 to 15 minutes)
- Exact guess: 3 points, off by one: 1 point
- A reminder resolves the bet

<div class="placeholder">TODO IMAGE: phone screenshot of the bet screen</div>

Note:
- Speaker: Lucas | Raffi

Demo step 3 (F1, F5). Audience action: bet on a delay. Show the timer while someone watches, the reminder otherwise. Bets survive a silo restart because the state is persisted and the reminder is durable. Live leaderboard by nickname.

---

## One request at a time

- Every grain has a mailbox, one message at a time
- No locks inside the grain
- The price: a slow call blocks the next ones
- Deadlock: A waits for B, B waits for A, timeout

<img src="./images/spiderman_deadlock.webp">

Note:
- Speaker: Lucas | Raffi

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
- Speaker: Lucas | Raffi

Demo step 4 (F4): everyone picks a stop, Hauptbahnhof gets hot, then show the fix. `StopBoardCacheGrain` is real (src/Features/Stops). `[StatelessWorker]` is the one case where one id has several activations. Do not quote throughput numbers, none were measured.

---

## The rate limiter grain

- Many reminders, one Linz AG server
- All requests go through one `SourceGrain`
- One shared rate limit, results cached in the grains
- Non-reentrant on purpose: it counts in order

<!-- TODO IMAGE: arrows from many StopGrains into one SourceGrain, then to the EFA server -->

Note:
- Speaker: Lucas | Raffi

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
- Speaker: Lucas | Raffi

In pgAdmin on the big screen, the audience sees the dead silo flip. Details in how-orleans-works.md (membership, when a silo dies). Postgres is one choice, Redis or Azure Table are other usual ones.

---

## Cluster: directory and placement

- Directory: distributed, in memory, split over all silos by hash ranges
- Normally one activation per grain
- Placement: where does a new activation go?
- Default since Orleans 9.2: resource optimized (before: random)

<!-- TODO IMAGE: hash ring diagram (Mermaid in how-orleans-works.md, "Who knows where a grain lives?") -->

Note:
- Speaker: Lucas | Raffi

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
- Speaker: Lucas | Raffi

Splitting a module into its own deployable later needs only a small host that calls its `Add...Module` and `Map...Endpoints`. Aspire AppHost starts Postgres, three Host replicas, the Vite dev server and pgAdmin. Splitting into one silo type per module is possible, not done.

---

## Live updates: observers and SSE

- Grains publish events to one hub grain
- Every silo subscribes one grain observer
- Subscriptions expire unless renewed
- Each replica pushes events to browsers as SSE

<!-- TODO IMAGE: diagram grain -> hub -> observer per silo -> SSE -> browser -->

Note:
- Speaker: Lucas | Raffi

`GET /api/cluster/events`. An event raised on any silo reaches a browser connected to any replica. A dead silo drops out by itself because its subscription expires. Deliberately not Orleans streams.

---

## Demo 3: break the cluster

- Vote to kill a silo
- Map dots change colour, membership flips
- Grains come back on the other silos
- Bets still resolve

<div class="placeholder">TODO IMAGE: screenshot of the map with silo colours, before and after</div>

Note:
- Speaker: Lucas | Raffi

Demo step 5 (F6, F7). Audience action: vote to kill a silo. The chaos button kills the replica that receives the request, only when `Cluster:EnableChaos` is true (the AppHost sets it for the demo). Needs the 3 replicas. Rehearse it, and keep a screen recording as a backup.



---

## Kubernetes and load test

- `UseKubernetesHosting()` from `Microsoft.Orleans.Hosting.Kubernetes`
- Pod labels `orleans/serviceId`, `orleans/clusterId`
- Downward API env vars, RBAC, probes, long grace period
- Load test with NBomber on replayed XML

Note:
- Speaker: Lucas | Raffi

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
- Speaker: Lucas | Raffi

Tests mirror `src`, plus module boundary tests. Recorded XML responses make tests and CI independent from Linz AG.

---

## Observability

- OpenTelemetry: Orleans meter and activity sources exported
- Aspire dashboard: resources, logs, traces, metrics
- Orleans Dashboard: silos, grains, activations

<!-- TODO IMAGE: Aspire dashboard screenshot with a trace -->

Note:
- Speaker: Lucas | Raffi

Show the Aspire dashboard (http://localhost:15000) in the demo. A distributed trace Stop -> Ingestion needs those calls instrumented, verify what the dashboard shows before promising it.

---

## Lessons learned

- Persist what must survive, let go of the rest
- Keep domain code free of Orleans, the grain stays thin
- One id, one actor: validate ids at the edge
- Break call cycles by design before reaching for `[Reentrant]`
- Think about versioning before the first deployment

Note:
- Speaker: Lucas | Raffi

Tie back to daily Orleans work. Keep these personal and honest, only claim what you have seen in production.

---

## Discussion

- Where would grains hurt you? When is a cache and a cron job better?
- One cluster for all contexts or one per context?
- Reminders or a scheduler such as Quartz or Hangfire?
- Grain state is a blob. Accept it or project into read models (CQRS)?
- How do you roll out a breaking state change safely?

Note:
- Speaker: Lucas | Raffi

Ask two or three, let the room choose. More questions if there is time: is polling a third party API from many grains polite, and where does the rate limit live? Where would an LLM agent per grain make sense, and where is it a gimmick?

---

## Free play

- Join with your phone
- Pick a stop
- Bet on a delay
- Climb the leaderboard

<div class="placeholder">QR CODE / URL placeholder: public/images/qr.svg and https://TODO-audience-url</div>

Note:
- Speaker: Lucas | Raffi

Demo step 7 (F4, F5, F9). Nicknames only, 2 to 20 characters, unique ignoring case. Scale target is about 100 phones, one stop each. The stats panel shows cache hits versus calls to Linz AG, active grains, hottest stops.

---

## Takeaways

- A virtual actor is an identity, memory and activations come and go
- Timers for the cheap, reminders for the important
- Hot grains need a design, not just more silos
- Persist less, version early

Note:
- Speaker: Lucas | Raffi

Four sentences, then back to discussion.

---

## Next Stop: Virtual Actors
## Microsoft Orleans on the Linz tram network

<div class="thanks">
  <ul>
    <li>Code + Slides: <a href="https://github.com/MhouneyLH/gleis-grain">https://github.com/MhouneyLH/gleis-grain</a></li>
    <li>Meetup: <a href="https://www.meetup.com/net-stammtisch-linz/events/317130107/">https://www.meetup.com/net-stammtisch-linz/events/317130107/</a></li>
    <li>Orleans docs: <a href="https://dotnet.github.io/orleans/docs/">https://dotnet.github.io/orleans/docs/</a></li>
  </ul>
  <img class="qr" src="/images/repo_qr_code.png" alt="QR code to the repository">
</div>

Note:
- Speaker: Lucas
