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

## Grain persistence<sup class="ref">1</sup>

<img class="persistence" src="/images/persistance_overview.png" alt="Overview of grain persistence">

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-persistence/#choose-a-provider">https://dotnet.github.io/orleans/docs/grains/grain-persistence/#choose-a-provider</a></div>

Note:
- Speaker: Lucas | Raffi

Orleans ships storage providers for many backends (Azure, Cosmos DB, DynamoDB, Firestore, ADO.NET, Redis, Memory for tests). We use ADO.NET with Postgres, one database per module. State is read when the grain activates and written when the grain asks for it.

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

## Concurrency: turn-based execution

<img class="diagram" src="/images/concurreny_turn_based.png" alt="Three callers send requests to one grain, the grain answers them one at a time">

<p class="small">One request at a time per grain: no locks, no data races inside a grain</p>

<img class="pengu" src="/images/sad_pengu.gif" alt="Sad penguin waiting in the queue">

Note:
- Speaker: Lucas | Raffi

Orleans runs the code of a grain in turns: one request at a time, a second request waits in the queue until the first turn is finished. That is why a grain needs no locks. The price: a slow call blocks everybody who waits behind it (next slides: reentrancy, ReadOnly, StatelessWorker). Ask: what happens while the grain awaits another call?

---

<h2 class="swap"><span class="fragment fade-out" data-fragment-index="3">Concurrency: Problem?</span><span class="fragment" data-fragment-index="3">Concurrency: Deadlock</span></h2>

<div class="codepair">
<div>
<p class="codetitle">StopGrain</p>
<pre class="cs"><code class="no-highlight" data-noescape><span class="l">public async Task&lt;Board&gt; GetBoardAsync()</span><span class="l">{</span><span class="l">    var trip = GrainFactory.GetGrain&lt;ITripGrain&gt;(tripId);</span><span class="l fragment custom hl-box" data-fragment-index="1">    var delay = await trip.GetDelayAsync();</span><span class="l">    return Build(delay);</span><span class="l">}</span></code></pre>
</div>
<div>
<p class="codetitle">TripGrain</p>
<pre class="cs"><code class="no-highlight" data-noescape><span class="l">public async Task&lt;int&gt; GetDelayAsync()</span><span class="l">{</span><span class="l">    var stop = GrainFactory.GetGrain&lt;IStopGrain&gt;(stopId);</span><span class="l fragment custom hl-box" data-fragment-index="2">    var board = await stop.GetBoardAsync();</span><span class="l">    return Estimate(board);</span><span class="l">}</span></code></pre>
</div>
</div>
<div class="memerow fragment" data-fragment-index="3">
<img src="/images/spiderman_deadlock.webp" alt="Two Spider-Men pointing at each other">
<p>Stop waits for Trip, Trip waits for Stop -> Deadlock, until the call times out</p>
</div>

Note:
- Speaker: Lucas | Raffi

Ask the room first: "Who sees the problem?" Give them a moment, then click. Click 1 marks the call from the stop to the trip, click 2 the call back from the trip to the stop, click 3 shows the result. Both grains are turn-based: while StopGrain awaits the trip, it does not accept other requests, so TripGrain's call back into StopGrain waits for a turn that never comes. Orleans reports a timeout after the response timeout (30 s by default). Next slides: the fixes (reentrancy, interleaving, a different call flow). This is an illustration: in the repo the stop reports to the trip with a one-way call (`ReportAsync` is `[OneWay]`), so there is no cycle.

---

## Deadlock: the call cycle

<img class="diagram" src="/images/conc_01_deadlock.png" alt="Sequence diagram: StopGrain awaits TripGrain, TripGrain calls back into StopGrain and waits in the queue until the 30 s timeout">

<p class="small">Stop waits for Trip, Trip waits for Stop: nothing moves until the timeout</p>

Note:
- Speaker: Lucas | Raffi

A awaits B, B calls back into A, but A is still busy waiting for B, so the call back sits in the queue. Nobody can proceed until the call times out (default response timeout 30 s). Same picture as the code on the last slide, now as a sequence.

---

## Fix 1: reentrancy

- `[Reentrant]` on the grain class
- While a call awaits, the grain accepts other requests
- Still one thread at a time: calls interleave only at `await`
- Price: state can change between two awaits

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">[Reentrant]</span><span class="l">public sealed class StopGrain : Grain, IStopGrain</span><span class="l">{</span><span class="l">    public async Task&lt;Board&gt; GetBoardAsync()</span><span class="l">    {</span><span class="l">        var delay = await trip.GetDelayAsync();</span><span class="l">        // while we wait here, other requests may run</span><span class="l">        return Build(delay);</span><span class="l">    }</span><span class="l">}</span></code></pre>

Note:
- Speaker: Lucas | Raffi

Fix for the deadlock: the stop awaits the trip, the trip calls back into the stop, and because the grain is reentrant it takes the call while it waits. There is still never more than one thread inside the grain, but two requests can be half done at the same time, so do not assume the state is unchanged after an await. Use it for the grain class, or prefer AlwaysInterleave on a single method.

---

## Fix 1: reentrancy, the picture

<img class="diagram" src="/images/conc_02_fix_reentrant.png" alt="Sequence diagram: with Reentrant the StopGrain accepts the call back from TripGrain while it waits">

<p class="small">With [Reentrant] the call back is accepted while the stop waits</p>

Note:
- Speaker: Lucas | Raffi

With `[Reentrant]` the grain takes the call back while it waits for the trip. It is still one thread, but calls interleave at every await, so the state can change in between.

---

## Fix 2: interleave one method

- `[AlwaysInterleave]` on an interface method
- Only this method may run while another request is waiting
- The rest of the grain stays strictly turn-based
- Good for reads that do not touch changing state

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">public interface IStopGrain : IGrainWithStringKey</span><span class="l">{</span><span class="l">    [AlwaysInterleave]</span><span class="l">    Task&lt;Board&gt; GetBoardAsync();</span><span class="l"> </span><span class="l">    Task&lt;int&gt; WatchAsync(string watcher);</span><span class="l">}</span></code></pre>

Note:
- Speaker: Lucas | Raffi

Smaller hammer than [Reentrant]: only the marked method interleaves with other requests, everything else stays turn-based. Here the call back into GetBoardAsync is allowed while the stop waits for the trip. Make sure the method is safe to run in the middle of another one.

---

## Fix 2: interleave one method, the picture

<img class="diagram" src="/images/conc_03_fix_always_interleave.png" alt="Diagram: only the AlwaysInterleave method GetBoardAsync passes the queue, other requests keep waiting">

<p class="small">Only the marked method passes the queue, the rest stays turn-based</p>

Note:
- Speaker: Lucas | Raffi

Smaller hammer: only the method marked `[AlwaysInterleave]` may run while another request waits. Everything else in the grain stays turn-based, so the risk is limited to that one method.

---

## Fix 3: change the call flow

- Do not call back: pass the data instead of asking for it
- `[OneWay]`: the caller does not wait for the answer
- No wait, no cycle, no deadlock
- This is what gleis-grain does: Stop reports to Trip

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">// Stops reports what it sees, it does not wait for an answer</span><span class="l">GrainFactory.GetGrain&lt;ITripGrain&gt;(departure.TripId.Value)</span><span class="l">    .ReportAsync(report)</span><span class="l">    .Ignore();</span><span class="l"> </span><span class="l">public interface ITripGrain : IGrainWithStringKey</span><span class="l">{</span><span class="l">    [OneWay]</span><span class="l">    Task ReportAsync(TripReport report);</span><span class="l">}</span></code></pre>

Note:
- Speaker: Lucas | Raffi

The best fix is a design without a cycle. In the repo, a stop reports every departure to its trip with a one-way call and does not wait (`ReportAsync` is `[OneWay]`, the call is `.Ignore()`d), and the trip never calls the stop. A lost report is fine, the next refresh brings another. Rule of thumb: events and reports go one way, questions go to one place.

---

## Fix 3: change the call flow, the picture

<img class="diagram" src="/images/conc_04_fix_oneway.png" alt="Diagram: Stop sends the report one-way to Trip, nothing comes back">

<p class="small">Best fix: no cycle. Stop reports to Trip and does not wait</p>

Note:
- Speaker: Lucas | Raffi

The best fix is no cycle: Stop reports to Trip one-way and does not wait, nothing comes back. This is what gleis-grain does.

---

## Which fix when?

| Fix | Scope | Risk | Use when |
|---|---|---|---|
| `[OneWay]` / no call back | the call | a lost message is lost | reports and events |
| `[AlwaysInterleave]` | one method | runs in the middle of others | a read that is safe any time |
| `[Reentrant]` | the whole grain | state can change at every await | last resort, simple grain |

Note:
- Speaker: Lucas | Raffi

Prefer a design without a cycle. Interleave only when you have to, and then as narrow as possible.

---

## Cheat sheet: slow grain?

<img class="diagram wide" src="/images/conc_05_cheatsheet.png" alt="Flow chart: grain slow? read-heavy, cycle between grains, one key too hot, must be global, otherwise profile the turn">


Note:
- Speaker: Lucas | Raffi

Grain slow? Follow the arrows: read-heavy means StatelessWorker or ReadOnly, a cycle means a reentrancy fix or OneWay, a too hot key means split or batch, and if it must be global keep one grain and add a cache. Otherwise profile the turn and make it shorter.

---

## ReadOnly: reads interleave

<img class="diagram" src="/images/conc_06_readonly.png" alt="Diagram: ReadOnly methods run together while a write method waits">

<p class="small">Reads run together, a write waits for them and they wait for it</p>

Note:
- Speaker: Lucas | Raffi

Methods marked `[ReadOnly]` may interleave with each other, so many reads run together. A normal (write) method still waits for them, and they wait for it.

---

## StatelessWorker: many copies

<img class="diagram" src="/images/conc_07_stateless_worker.png" alt="Diagram: a StatelessWorker has several local copies per silo, before and after">

<p class="small">Several local copies per silo, served by the local silo</p>

Note:
- Speaker: Lucas | Raffi

A `[StatelessWorker]` is not one single activation: Orleans creates several local copies per silo (up to the CPU core count by default) and calls are served by the local silo. That fits caches and read-heavy lookups. Here `StopBoardCacheGrain` caches the board for a few seconds in front of `StopGrain`.

---

## Hot grain: Hauptbahnhof

<img class="diagram" src="/images/conc_08_hot_grain.png" alt="Diagram: many phones pick the same stop, one StopGrain takes every call while other silos idle">

<p class="small">One stop, one activation, one mailbox: everybody waits there</p>

Note:
- Speaker: Lucas | Raffi

About 100 phones pick the same stop, so one StopGrain on one silo takes every call while the other silos idle. First ask: is the key a bad architectural decision, or can we batch and cache?

---

## SourceGrain: one global gateway

<img class="diagram" src="/images/conc_09_source_grain.png" alt="Diagram: many StopGrains call one SourceGrain which calls the Linz AG server">

<p class="small">One grain is right here: one rate limit for the whole cluster</p>

Note:
- Speaker: Lucas | Raffi

Here one grain is right: a single SourceGrain is the one rate limiter for the whole cluster, with a minimum of 500 ms between calls to the Linz AG server and results cached per stop for 20 s. The price: grain chains block each other, so one slow server call delays every caller. The cache and the stale fallback soften this.

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

- `SchemaVersion` field exists, migration in `OnActivateAsync` is still missing
- `[Version(2)]` on grain interfaces
- Compatibility strategy and version selector
- v1 and v2 silos coexist during a rolling update

<div class="placeholder">timeline of a rolling update with v1 and v2 silos</div>
<!-- TODO IMAGE: timeline of a rolling update with v1 and v2 silos, name: rolling_update.svg -->

Note:
- Speaker: Lucas

In the repo: `IStopGrain` has `[Version(1)]` in the v1 build and `[Version(2)]` in the v2 build (`-p:GrainVersion=2`), v2 only adds (a `GrainVersion` field on the board and one method). The cluster uses the BackwardCompatible strategy and the LatestVersion selector. Still PLANNED, NOT BUILT: the `SchemaVersion` migration in `OnActivateAsync` (the field exists, no migration code). Discussion question: how do you roll out a breaking state change safely?

---

## Versioning: how to read the map

- Colour = grain version: **red** v1, **blue** v2
- Shape = silo: square, circle, triangle
- Size = watchers

<div class="placeholder">screenshot: map legend, red and blue dots in square, circle and triangle shapes</div>
<!-- TODO IMAGE: screenshot of the map legend (red v1, blue v2; square, circle, triangle = silo), name: map_legend.png -->

Note:
- Speaker: Lucas

Built in the frontend (shape = silo slot, colour = grain version, size = watchers); checked on screenshots against a mock API, not yet against the real cluster. Look at it once with the real rollout before the talk.

---

## Demo: rolling update

- Red dots: everything runs v1
- Start v2 next to v1
- Stop v1 one by one
- Dots turn blue, the app keeps answering

<div class="placeholder">screenshot: Aspire dashboard with gleis-grain-v1 and gleis-grain-v2 side by side</div>
<!-- TODO IMAGE: screenshot of the Aspire dashboard with the gleis-grain-v1-* and gleis-grain-v2-* rows, name: aspire_v1_v2.png -->

Note:
- Speaker: Lucas | Raffi

Built and run end to end in a headless AppHost (v1 and v2 silos in one cluster, grains moved to v2, calls kept working). Not yet tried: the clicks in the dashboard itself and the frontend against the gateway. Rehearse once. Slot: 5 min.

Before the talk (not on stage):
- Start with `dotnet run --project src/AppHost --launch-profile replay`. The Aspire dashboard shows 3 rows `gleis-grain-v1-*` (running) and 3 rows `gleis-grain-v2-*` (NotStarted). The app and the frontend use one stable address, the `gleis-grain` gateway on http://localhost:5080, during the whole rollout.
- v2 is built automatically into `artifacts/v2` with every AppHost build (by hand: `dotnet build src/Host -p:GrainVersion=2 --artifacts-path artifacts/v2`).
- Ask the audience to keep several stops open, so many grains are active (otherwise nothing to see).

On stage:
1. (0:30) Projector page: all dots red, three shapes. Say: "Everything runs version 1, three silos."
2. (0:30) Aspire dashboard http://localhost:15000: click start on each of the three `gleis-grain-v2-*` rows (one click per replica, so it comes up gradually). Say: "Now the new version comes up next to the old one, nobody is switched off."
3. (0:45) Show the silo list: 6 active silos, v1 and v2 side by side (silo panel on the projector page, or `/api/cluster/silos`, or the Orleans Dashboard at /dashboard). Most grains already turn blue as soon as v2 callers arrive. Say: "One cluster, two versions."
4. (1:15) Explain Orleans interface versioning: `[Version(2)]` on the interface, BackwardCompatible strategy, LatestVersion selector. Say: "New activations go to v2, v1 callers still work, the cluster does not care."
5. (1:30) Click stop on the three `gleis-grain-v1-*` rows one by one. The v1 silos go Dead (an abrupt kill, not a graceful shutdown, so one or two requests may hang for a moment). Watch grains move to other silos and dots turn blue, while the app keeps answering (tap a stop on the phone to prove it). Say: "A rolling update without a maintenance window."
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
