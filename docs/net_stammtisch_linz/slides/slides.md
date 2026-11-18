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
    <p>Backend Engineer by day, Hobby Platform Engineer by night</p>
    <p>Orleans is part of daily work</p>
    <p>Cloud Native, OpenTelemetry, .NET</p>
  </div>
  <div class="speaker">
    <img src="/images/raffi.jpg" alt="Raffael Rehberger">
    <h3>Raffael Rehberger</h3>
    <p>Team Lead, Cloud Architect</p>
    <p>Orleans is part of daily work</p>
    <p>F(L)OSS Software, Music Production, Linux</p>
  </div>
</div>

Note:
- Speaker: Lucas | Raffi

---

## Agenda

1. Project intro and showcase
2. Actors, grains and state
3. Cluster, timers and reminders
4. Concurrency and hot spots
5. Versioning
6. Kubernetes and testing
7. Take this home & try it out

Note:
- Speaker: Lucas

Timings in minutes (start-end of the talk):
- Project intro and showcase: 5 min (0-5)
- Actors, grains and state (what are actors ... and why, grains and state): 10 min (5-15)
- Cluster, timers and reminders (cluster in action 5 min, timers vs reminders 3 min): 8 min (15-23)
- Concurrency and hot spots: 5 min (23-28)
- Versioning: 5 min (28-33)
- Kubernetes and testing (Kubernetes is the bridge to load tests and testing in general): 6 min (33-39)
- Take this home & try it out: 2 min (39-41)
- Questions: 5 min (41-46)

Total: 46 min of 45. Over budget by 1 min, trim something.

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

## What you will see

- The map on the projector
- 80 simulated people pick stops
- We break the cluster
- A rolling version update

<p class="small">Map data © OpenStreetMap contributors<sup class="ref">1</sup></p>

<div class="refs"><sup class="ref">1</sup> <a href="https://www.openstreetmap.org/copyright">https://www.openstreetmap.org/copyright</a></div>


Note:
- Speaker: Lucas | Raffi

Shape = silo is the design for the demo, colour = grain version belongs to the versioning demo (PLANNED, not built yet, see versioning). Public OpenStreetMap tiles are fine for a demo, the attribution has to stay visible.

---

## Demo: Showcase

Note:
- Speaker: Lucas | Raffi

About 2 min. We only use localhost, so the audience cannot join from their phones. An audience simulator (resource `simulator` in the Aspire dashboard, not started automatically) acts like about 80 people through the public API. Be honest: say they are simulated people. What to do (steps marked PLAN depend on the frontend being finished):
1. Before the talk: `dotnet run --project src/AppHost`. If the Wi-Fi is bad: `dotnet run --project src/AppHost --launch-profile replay` (recorded departures, no Linz AG calls).
2. Open the Aspire dashboard http://localhost:15000. Say: "Three replicas, one Orleans silo each, one cluster."
3. Open the projector page `/projector` and make it full screen on the beamer. Say: "Every dot is a stop, every stop is a grain."
4. In the Aspire dashboard click start on `simulator`. Say: "Now 80 people join, simulated ones, they come in over about 40 seconds."
5. Dots light up and grow with the number of watchers. About 40 percent watch Hauptbahnhof, the rest spread over the 25 stops, and they move around and read boards. Say: "Nobody created these actors, the first call activated them."
6. Point at Hauptbahnhof. Say: "One stop gets hot, we come back to this."
7. Point at the silo shapes and the watcher bars. Say: "The shape tells you which silo hosts the grain." (PLAN: shape = silo is not built yet, confirm.)

If it fails: if the simulator does not start, start it again from the dashboard. Restart with `--launch-profile replay`; if the app itself is down, play the pre-recorded video (TODO: record it, keep it local on the laptop) and keep talking.

---

## What are actors? ... and why??? II

![Actors in Orleans](images/grains.png)

---

## Stop Grain (simplified)<sup class="ref">1</sup>

```csharp
[GrainType("stop")]
public class StopGrain(IOptions<StopsOptions> options, ILogger<StopGrain> logger) : Grain, IStopGrain {
    private Stop? _stop;

    public override Task OnActivateAsync(CancellationToken ct) {
        ...
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken ct) {
        ...
    }

    public async Task<StopBoardSnapshot?> GetBoardAsync() {
        ... 
    }

    public async Task<int> WatchAsync(string watcher) {
        ...
    }

    public async Task<int> UnwatchAsync(string watcher) {
        ...
    }
```

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-lifecycle/">https://dotnet.github.io/orleans/docs/grains/grain-lifecycle/</a></div>

---

## A grain with persistent state<sup class="ref">1</sup>

- `IPersistentState<StopState>`, named storage, Postgres behind it
- State is a blob, one generic table<sup class="ref">2</sup>
- Domain class `Stop`: plain C#, no Orleans
- Grain: thin wrapper, load, call, save

```csharp
public sealed class StopGrain(
    [PersistentState("stop", StopsOptions.StorageName)]
    IPersistentState<StopState> state, ...) : Grain, IStopGrain
```

<div class="refs">
  <div><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-persistence/">https://dotnet.github.io/orleans/docs/grains/grain-persistence/</a></div>
  <div><sup class="ref">2</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-persistence/relational-storage/">https://dotnet.github.io/orleans/docs/grains/grain-persistence/relational-storage/</a></div>
</div>

Note:
- Speaker: Lucas | Raffi

In the repo: `StopGrain` with `StopState` (src/Features/Stops). The domain class can be unit tested without a silo. Restart a silo in the demo: the state is still there (demo step 2). Write-behind (do not persist every refresh) and `[OneWay]` are talking points only, not implemented.

---

## Grain persistence<sup class="ref">1</sup>

<img class="persistence" src="/images/persistance_overview.png" alt="Overview of grain persistence">

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-persistence/#choose-a-provider">https://dotnet.github.io/orleans/docs/grains/grain-persistence/#choose-a-provider</a></div>

Note:
- Speaker: Lucas | Raffi

Orleans ships storage providers for many backends (Azure, Cosmos DB, DynamoDB, Firestore, ADO.NET, Redis, Memory for tests). We use ADO.NET with Postgres, one database per module. State is read when the grain activates and written when the grain asks for it.

---

## Virtual actors in Orleans<sup class="ref">1</sup>

- A **grain** is an object with an identity and its own state<sup class="ref">2</sup>
- You never create or destroy it, you just call it
- Orleans activates it on the first call, forgets it when idle
- It *always exists logically*, like virtual memory

```csharp
var stop = grains.GetGrain<IStopGrain>(stopId);
var board = await stop.GetBoardAsync();
```

<div class="refs">
  <div><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/overview/#the-virtual-actor-model">https://dotnet.github.io/orleans/docs/overview/#the-virtual-actor-model</a></div>
  <div><sup class="ref">2</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-identity/">https://dotnet.github.io/orleans/docs/grains/grain-identity/</a></div>
</div>

Note:
- Speaker: Raffi

Careful: the snippet is illustrative, check the real method names in `Endpoints/GetStop.cs` before showing code. Office analogy from how-orleans-works.md: grain = clerk for one stop, silo = building, cluster = all buildings, activation = clerk at the desk. A typo in an id silently gets its own empty grain, so validate ids at the edge.

---

## Grains, Silos, Cluster<sup class="ref">1</sup>

| Orleans    | Office                              |
|------------|-------------------------------------|
| Grain      | Clerk for exactly one stop          |
| Silo       | An office building (a .NET process) |
| Cluster    | All buildings                       |
| Activation | Clerk at the desk right now         |

<!-- TODO IMAGE: diagram of the cluster with silos and grains (Mermaid source in how-orleans-works.md, "The big picture") -->

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/overview/#silos-and-clusters">https://dotnet.github.io/orleans/docs/overview/#silos-and-clusters</a></div>

Note:
- Speaker: Lucas

Vertical slide, can be skipped. Location transparency: you never say which silo.

---

## Cluster in Action

![cluster](/images/cluster.png)

- Grain Placement<sup class="ref">1</sup>
- Grain Communication

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-placement/">https://dotnet.github.io/orleans/docs/grains/grain-placement/</a></div>

Note:
- Speaker: Raffi

---

## Timers vs. Reminders - The Timer<sup class="ref">1</sup>

```csharp
protected IGrainTimer RegisterGrainTimer<TState>(
    Func<TState, CancellationToken, Task> callback, // function invoked when the timer ticks
    TState state,                                   // object to pass to callback
    GrainTimerCreationOptions options)              // timer creation options
```

- only for current activation
- standard System.Threading.Timer
- non persistent
- high precision (millisecond ticks)

---

## Timer vs. Reminders - The Reminder<sup class="ref">2</sup>

- reminders are persistent across activations
- less precision (minutes, days, weeks, months)

<div class="refs">
  <div><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/timers/">https://dotnet.github.io/orleans/docs/grains/timers/</a></div>
  <div><sup class="ref">2</sup> <a href="https://dotnet.github.io/orleans/docs/grains/reminders/">https://dotnet.github.io/orleans/docs/grains/reminders/</a></div>
</div>

---

## Concurrency and hot spots

- turn-based execution
- interleave & reentrancy
- readonly
- stateless worker
- hot grain: Hauptbahnhof -> bad architectural decision or batching
    - sourcegrain -> grain chains block each other, one global grain; show at rate limiter

<p class="small">Two kinds of work: changing state (calls block each other) and reading (reads should not wait behind writes)</p>

Note:
- Speaker: Lucas

Structure of this block: first how a grain works (turn-based). Part A, changing state: what can go wrong when grains call each other (deadlock) and the three fixes (reentrancy, interleave one method, change the call flow). Part B, reading: reads should not wait in the queue behind writes, so `[ReadOnly]` and then `[StatelessWorker]`. Part C, hot spots: the hot grain Hauptbahnhof and the SourceGrain, each as before and after, then the cheat sheet. Two kinds of work: reading vs changing state.

---

## Concurrency: turn-based execution<sup class="ref">1</sup>

<img class="diagram" src="/images/concurreny_turn_based.png" alt="Three callers send requests to one grain, the grain answers them one at a time">

<p class="small">One request at a time per grain: no locks, no data races inside a grain</p>

<img class="pengu fragment" src="/images/sad_pengu.gif" alt="Sad penguin waiting in the queue">

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/">https://dotnet.github.io/orleans/docs/grains/request-scheduling/</a></div>

Note:
- Speaker: Lucas | Raffi

Orleans runs the code of a grain in turns: one request at a time, a second request waits in the queue until the first turn is finished. That is why a grain needs no locks. The price: a slow call blocks everybody who waits behind it (next slides: reentrancy, ReadOnly, StatelessWorker). Ask: what happens while the grain awaits another call?

---

## Reading vs changing state

| Reading | Changing state |
|---|---|
| `[ReadOnly]` | turn-based execution |
| `[StatelessWorker]` | `[Reentrant]` |
| caches | `[AlwaysInterleave]` |
| | `[OneWay]` |

Note:
- Speaker: Lucas | Raffi

Two kinds of work. Changing state needs the grain to be careful: turn-based execution protects the state, and the tools on the right (Reentrant, AlwaysInterleave, OneWay) are about calls that block each other. Reading is different: many readers should not queue behind a writer, that is what ReadOnly, StatelessWorker and caches are for. Order of the next slides: Part A changing state (deadlock and the three fixes), Part B reading (ReadOnly, StatelessWorker), Part C hot spots (Hauptbahnhof, SourceGrain).

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

Part A, changing state: what can go wrong when grains call each other.

Ask the room first: "Who sees the problem?" Give them a moment, then click. Click 1 marks the call from the stop to the trip, click 2 the call back from the trip to the stop, click 3 shows the result. Both grains are turn-based: while StopGrain awaits the trip, it does not accept other requests, so TripGrain's call back into StopGrain waits for a turn that never comes. Orleans reports a timeout after the response timeout (30 s by default). Next slides: the fixes (reentrancy, interleaving, a different call flow). This is an illustration: in the repo the stop reports to the trip with a one-way call (`ReportAsync` is `[OneWay]`), so there is no cycle.

---

## Deadlock: the call cycle

<img class="diagram" src="/images/conc_01_deadlock.png" alt="Sequence diagram: StopGrain awaits TripGrain, TripGrain calls back into StopGrain and waits in the queue until the 30 s timeout">

<p class="small">Stop waits for Trip, Trip waits for Stop: nothing moves until the timeout</p>

Note:
- Speaker: Lucas | Raffi

A awaits B, B calls back into A, but A is still busy waiting for B, so the call back sits in the queue. Nobody can proceed until the call times out (default response timeout 30 s). Same picture as the code on the last slide, now as a sequence.

---

## Fix 1: reentrancy<sup class="ref">1</sup>

- `[Reentrant]` on the grain class
- While a call awaits, the grain accepts other requests
- Still one thread at a time: calls interleave only at `await`
- Price: state can change between two awaits

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">[Reentrant]</span><span class="l">public sealed class StopGrain : Grain, IStopGrain</span><span class="l">{</span><span class="l">    public async Task&lt;Board&gt; GetBoardAsync()</span><span class="l">    {</span><span class="l">        var delay = await trip.GetDelayAsync();</span><span class="l">        // while we wait here, other requests may run</span><span class="l">        return Build(delay);</span><span class="l">    }</span><span class="l">}</span></code></pre>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/#reentrant-grains">https://dotnet.github.io/orleans/docs/grains/request-scheduling/#reentrant-grains</a></div>

Note:
- Speaker: Lucas | Raffi

Fix for the deadlock: the stop awaits the trip, the trip calls back into the stop, and because the grain is reentrant it takes the call while it waits. There is still never more than one thread inside the grain, but two requests can be half done at the same time, so do not assume the state is unchanged after an await. Use it for the grain class, or prefer AlwaysInterleave on a single method.

---

## Fix 1: reentrancy<sup class="ref">1</sup>

<img class="diagram" src="/images/conc_02_fix_reentrant.png" alt="Sequence diagram: with Reentrant the StopGrain accepts the call back from TripGrain while it waits">

<p class="small">With [Reentrant] the call back is accepted while the stop waits</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/#reentrant-grains">https://dotnet.github.io/orleans/docs/grains/request-scheduling/#reentrant-grains</a></div>

Note:
- Speaker: Lucas | Raffi

With `[Reentrant]` the grain takes the call back while it waits for the trip. It is still one thread, but calls interleave at every await, so the state can change in between.

---

## Fix 2: interleave one method<sup class="ref">1</sup>

- `[AlwaysInterleave]` on an interface method
- Only this method may run while another request is waiting
- The rest of the grain stays strictly turn-based
- Good for reads that do not touch changing state

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">public interface IStopGrain : IGrainWithStringKey</span><span class="l">{</span><span class="l">    [AlwaysInterleave]</span><span class="l">    Task&lt;Board&gt; GetBoardAsync();</span><span class="l"> </span><span class="l">    Task&lt;int&gt; WatchAsync(string watcher);</span><span class="l">}</span></code></pre>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/#method-level-interleaving">https://dotnet.github.io/orleans/docs/grains/request-scheduling/#method-level-interleaving</a></div>

Note:
- Speaker: Lucas | Raffi

Smaller hammer than [Reentrant]: only the marked method interleaves with other requests, everything else stays turn-based. Here the call back into GetBoardAsync is allowed while the stop waits for the trip. Make sure the method is safe to run in the middle of another one.

---

## Fix 2: interleave one method<sup class="ref">1</sup>

<img class="diagram" src="/images/conc_03_fix_always_interleave.png" alt="Diagram: only the AlwaysInterleave method GetBoardAsync passes the queue, other requests keep waiting">

<p class="small">Only the marked method passes the queue, the rest stays turn-based</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/#method-level-interleaving">https://dotnet.github.io/orleans/docs/grains/request-scheduling/#method-level-interleaving</a></div>

Note:
- Speaker: Lucas | Raffi

Smaller hammer: only the method marked `[AlwaysInterleave]` may run while another request waits. Everything else in the grain stays turn-based, so the risk is limited to that one method.

---

## Fix 3: change the call flow<sup class="ref">1</sup>

- Do not call back: pass the data instead of asking for it
- `[OneWay]`: the caller does not wait for the answer
- No wait, no cycle, no deadlock
- This is what gleis-grain does: Stop reports to Trip

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">// Stops reports what it sees, it does not wait for an answer</span><span class="l">GrainFactory.GetGrain&lt;ITripGrain&gt;(departure.TripId.Value)</span><span class="l">    .ReportAsync(report)</span><span class="l">    .Ignore();</span><span class="l"> </span><span class="l">public interface ITripGrain : IGrainWithStringKey</span><span class="l">{</span><span class="l">    [OneWay]</span><span class="l">    Task ReportAsync(TripReport report);</span><span class="l">}</span></code></pre>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/oneway/">https://dotnet.github.io/orleans/docs/grains/oneway/</a></div>

Note:
- Speaker: Lucas | Raffi

The best fix is a design without a cycle. In the repo, a stop reports every departure to its trip with a one-way call and does not wait (`ReportAsync` is `[OneWay]`, the call is `.Ignore()`d), and the trip never calls the stop. A lost report is fine, the next refresh brings another. Rule of thumb: events and reports go one way, questions go to one place.

---

## Fix 3: change the call flow<sup class="ref">1</sup>

<img class="diagram" src="/images/conc_04_fix_oneway.png" alt="Diagram: Stop sends the report one-way to Trip, nothing comes back">

<p class="small">Best fix: no cycle. Stop reports to Trip and does not wait</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/oneway/">https://dotnet.github.io/orleans/docs/grains/oneway/</a></div>

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

## ReadOnly: reads interleave

- `[ReadOnly]` on an interface method that only reads
- Reads may run together, one does not wait for the other
- A normal (write) method still waits for them, and they for it
- Price: the method must not change the grain's state

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">public interface IStopGrain : IGrainWithStringKey</span><span class="l">{</span><span class="l">    [ReadOnly]</span><span class="l">    Task&lt;StopBoardSnapshot?&gt; GetBoardAsync();</span><span class="l"> </span><span class="l">    Task&lt;int&gt; WatchAsync(string watcher);</span><span class="l">}</span></code></pre>

Note:
- Speaker: Lucas | Raffi

Part B, reading: reads should not wait in the queue behind writes. `[ReadOnly]` marks a method that does not change state, so many of them run at the same time (they interleave only with each other). Sketch of the idea, not in the repo as it is: the real `GetBoardAsync` can refresh a stale board, which changes state, so it could not be `[ReadOnly]` without splitting the refresh out.

---

## ReadOnly: reads interleave<sup class="ref">1</sup>

<img class="diagram" src="/images/conc_06_readonly.png" alt="Diagram: ReadOnly methods run together while a write method waits">

<p class="small">Reads run together, a write waits for them and they wait for it</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/">https://dotnet.github.io/orleans/docs/grains/request-scheduling/</a></div>

Note:
- Speaker: Lucas | Raffi

Methods marked `[ReadOnly]` may interleave with each other, so many reads run together. A normal (write) method still waits for them, and they wait for it.

---

## StatelessWorker: many copies<sup class="ref">1</sup>

- `[StatelessWorker]` on the grain class
- Orleans creates several copies per silo, calls are served locally
- No state of its own that has to stay in sync
- Good for caches and lookups, copies ask the real grain when needed

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">[StatelessWorker]</span><span class="l">public class StopBoardCacheGrain : Grain, IStopBoardCacheGrain</span><span class="l">{</span><span class="l">    // one id, but many activations per silo</span><span class="l">    public Task&lt;StopBoardSnapshot?&gt; GetBoardAsync() { ... }</span><span class="l">}</span></code></pre>

Note:
- Speaker: Lucas | Raffi

`[StatelessWorker]` is the one case where one id has several activations (up to the CPU core count per silo by default, `[StatelessWorker(n)]` limits it). A copy is never the source of truth: it may only hold data it can rebuild. The real `StopBoardCacheGrain` follows in part C.

---

## StatelessWorker: many copies

<img class="diagram" src="/images/conc_07_stateless_worker.png" alt="Diagram: a StatelessWorker has several local copies per silo, before and after">

<p class="small">Several local copies per silo, served by the local silo</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/stateless-worker-grains/">https://dotnet.github.io/orleans/docs/grains/stateless-worker-grains/</a></div>

Note:
- Speaker: Lucas | Raffi

A `[StatelessWorker]` is not one single activation: Orleans creates several local copies per silo (up to the CPU core count by default) and calls are served by the local silo. That fits caches and read-heavy lookups. Here `StopBoardCacheGrain` caches the board for a few seconds in front of `StopGrain`.

---

## Hot grain: Hauptbahnhof

<img class="diagram" src="/images/conc_12_hot_grain_before_after.png" alt="Before: 100 phones all call one StopGrain and wait in its mailbox. After: stateless board cache copies per silo serve the phones and ask the StopGrain once every 5 seconds">

<p class="small">Before: one mailbox for everybody. After: cache copies per silo, the stop is asked every few seconds</p>

Note:
- Speaker: Lucas | Raffi

Part C, hot spots. About 100 simulated people pick the same stop, so one StopGrain on one silo takes every call while the other silos idle (one stop, one activation, one mailbox). First ask: is the key a bad architectural decision, or can we cache and batch? Right side: a `[StatelessWorker]` board cache, several copies per silo, each keeps the last snapshot for a few seconds.

---

## Hot grain: Hauptbahnhof

- Problem: one grain takes the reads of about 100 watchers
- Fix: stateless copies per silo + a short TTL cache
- Price: boards are a few seconds old
- Alternatives: split the key, batch the writes

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">[StatelessWorker]</span><span class="l">public class StopBoardCacheGrain : Grain, IStopBoardCacheGrain</span><span class="l">{</span><span class="l">    public async Task&lt;StopBoardSnapshot?&gt; GetBoardAsync()</span><span class="l">    {</span><span class="l">        if (_snapshot is not null &amp;&amp; now - _cachedAt &lt; options.Value.BoardCacheTtl)</span><span class="l">            return _snapshot;                        // served by this copy</span><span class="l">        var stop = GrainFactory.GetGrain&lt;IStopGrain&gt;(this.GetPrimaryKeyString());</span><span class="l">        _snapshot = await stop.GetBoardAsync();      // only when expired</span><span class="l">        _cachedAt = now;</span><span class="l">        return _snapshot;</span><span class="l">    }</span><span class="l">}</span><span class="l">// switch: "Stops": { "UseBoardCache": true, "BoardCacheTtl": "00:00:05" }</span></code></pre>

Note:
- Speaker: Lucas | Raffi

Simplified from `StopBoardCacheGrain` (src/Features/Stops). Key = stop id, TTL 5 s by default. Switch it with `Stops:UseBoardCache` (default `false` in appsettings.json): demo it off and on while the simulator runs and watch the stop grain's calls. Watching still goes straight to the stop grain. The price is honest: boards are a few seconds old. Alternatives if the key itself is the problem: split the key (per line or platform), or batch writes (apply once per second). Do not quote throughput numbers, none were measured.

---

## SourceGrain: one global gateway

<img class="diagram" src="/images/conc_13_source_grain_before_after.png" alt="Before: every grain calls the Linz AG server directly. After: all grains go through one SourceGrain with a rate limit of 500 ms, a cache per stop of 20 s and a stale fallback">

<p class="small">Before: every grain hits the server. After: one gateway with rate limit, cache and stale fallback</p>

Note:
- Speaker: Lucas | Raffi

Before: many grains and reminders call the Linz AG server directly, nobody limits them, so we hammer it. After: everything goes through ONE SourceGrain: a minimum of 500 ms between calls, results cached per stop for 20 s, and a stale answer if the feed fails. Honest price: one global grain is a chokepoint. Grain chains block each other, a slow call delays everybody who waits behind it. The cache and the stale fallback soften this, because most calls never reach the server.

---

## SourceGrain: one global gateway

- Cache first: a fresh answer never reaches the server
- Rate limit: wait until 500 ms have passed since the last call
- Then one call to the Linz AG feed
- Feed fails: serve the stale answer

<pre class="cs single"><code class="no-highlight" data-noescape><span class="l">if (cache.TryGetFresh(key, now, out var fresh))</span><span class="l">    return fresh;                            // per stop, 20 s</span><span class="l">try</span><span class="l">{</span><span class="l">    var wait = _rateLimiter.Reserve(now);    // min 500 ms between calls</span><span class="l">    if (wait &gt; TimeSpan.Zero) await Task.Delay(wait);</span><span class="l">    var value = await fetch();               // the call to Linz AG</span><span class="l">    cache.Set(key, value, now);</span><span class="l">    return value;</span><span class="l">}</span><span class="l">catch (Exception) when (cache.TryGetStale(key, out var stale))</span><span class="l">{</span><span class="l">    return stale;                            // feed failed: old data beats none</span><span class="l">}</span></code></pre>

Note:
- Speaker: Lucas | Raffi

Simplified from `GetAsync` in `SourceGrain` (src/Features/Ingestion). The grain handles one call at a time, so the rate limiter needs no lock: that is why it is deliberately not reentrant. Cache and counters live in memory on purpose. If the feed fails and there is no stale value, the exception goes to the caller (the slide leaves out the logging and the counters). Linz AG terms of use and rate limits are still unchecked, keep polling polite.

---

## Cheat sheet: slow grain?

<img class="diagram wide" src="/images/conc_05_cheatsheet.png" alt="Flow chart: grain slow? read-heavy, cycle between grains, one key too hot, must be global, otherwise profile the turn">


Note:
- Speaker: Lucas | Raffi

Grain slow? Follow the arrows: read-heavy means StatelessWorker or ReadOnly, a cycle means a reentrancy fix or OneWay, a too hot key means split or batch, and if it must be global keep one grain and add a cache. Otherwise profile the turn and make it shorter.

---

## Versioning: serialization<sup class="ref">1</sup>

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

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/host/configuration-guide/serialization/#versioning-rules">https://dotnet.github.io/orleans/docs/host/configuration-guide/serialization/#versioning-rules</a></div>

Note:
- Speaker: Lucas

This is in the repo on state, snapshots, ids and grain interfaces. Snippet shortened from the real `StopState` (more fields with ids 2 and 3). State is a blob and old blobs stay across deployments, so this matters a lot.

---

## Versioning: rolling updates

- `SchemaVersion` field exists, migration in `OnActivateAsync` is still missing
- `[Version(2)]` on grain interfaces<sup class="ref">1</sup>
- Compatibility strategy and version selector
- v1 and v2 silos coexist during a rolling update<sup class="ref">2</sup>

<div class="refs">
  <div><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-versioning/grain-versioning/">https://dotnet.github.io/orleans/docs/grains/grain-versioning/grain-versioning/</a></div>
  <div><sup class="ref">2</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-versioning/deploying-new-versions-of-grains/">https://dotnet.github.io/orleans/docs/grains/grain-versioning/deploying-new-versions-of-grains/</a></div>
</div>

Note:
- Speaker: Lucas

In the repo: `IStopGrain` has `[Version(1)]` in the v1 build and `[Version(2)]` in the v2 build (`-p:GrainVersion=2`), v2 only adds (a `GrainVersion` field on the board and one method). The cluster uses the BackwardCompatible strategy and the LatestVersion selector. Still PLANNED, NOT BUILT: the `SchemaVersion` migration in `OnActivateAsync` (the field exists, no migration code). Discussion question: how do you roll out a breaking state change safely?

---

## Versioning: rolling updates<sup class="ref">1</sup>

<img class="diagram" src="/images/ver_10_rolling_update.png" alt="Three stages: only v1 silos, v2 silos join the same cluster, v1 silos stopped; red is v1, blue is v2, the shape is the silo">

<p class="small">Start the v2 replicas, then stop the v1 replicas: one cluster the whole time</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-versioning/deploying-new-versions-of-grains/#rolling-upgrade">https://dotnet.github.io/orleans/docs/grains/grain-versioning/deploying-new-versions-of-grains/#rolling-upgrade</a></div>

Note:
- Speaker: Lucas | Raffi

Read it top to bottom. First only v1 silos run, then three v2 silos join the same cluster, and during that time both versions are in one cluster (BackwardCompatible strategy, LatestVersion selector). Then we stop the v1 silos. Grains that lived on a stopped silo are activated again on a v2 silo at their next call.

---

## Versioning: compatibility<sup class="ref">1</sup>

<img class="diagram" src="/images/ver_11_version_compatibility.png" alt="A v1 caller can call a v2 grain (BackwardCompatible), a new activation goes to a v2 silo (LatestVersion), v2 only adds methods and fields with new ids">

<p class="small">v2 only adds, so v1 callers keep working while the rollout runs</p>

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-versioning/version-selector-strategy/">https://dotnet.github.io/orleans/docs/grains/grain-versioning/version-selector-strategy/</a></div>

Note:
- Speaker: Lucas | Raffi

BackwardCompatible means a v1 caller may call a v2 grain, which is why the rollout can run under load. LatestVersion means a new activation is placed on a v2 silo. This only holds if v2 just adds: a new method, a new field with a new `[Id]`. Never reuse or retype an id.

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

## Demo: Versioning

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
- Click start on `simulator` beforehand, so many stop grains are active (otherwise nothing to see).

On stage:
1. (0:30) Projector page: all dots red, three shapes. Say: "Everything runs version 1, three silos."
2. (0:30) Aspire dashboard http://localhost:15000: click start on each of the three `gleis-grain-v2-*` rows (one click per replica, so it comes up gradually). Say: "Now the new version comes up next to the old one, nobody is switched off."
3. (0:45) Show the silo list: 6 active silos, v1 and v2 side by side (silo panel on the projector page, or `/api/cluster/silos`, or the Orleans Dashboard at /dashboard). Most grains already turn blue as soon as v2 callers arrive. Say: "One cluster, two versions."
4. (1:15) Explain Orleans interface versioning: `[Version(2)]` on the interface, BackwardCompatible strategy, LatestVersion selector. Say: "New activations go to v2, v1 callers still work, the cluster does not care."
5. (1:30) Click stop on the three `gleis-grain-v1-*` rows one by one. The v1 silos go Dead (an abrupt kill, not a graceful shutdown, so one or two requests may hang for a moment). Watch grains move to other silos and dots turn blue, while the app keeps answering (open a stop on the projector page or laptop to prove it, the simulator keeps going). Say: "A rolling update without a maintenance window."
6. (0:30) Take-away: "Interface versions plus a compatible state format make deployments boring. Think about it before the first deployment." Back to Take this home.

If it fails:
- Play the pre-recorded video (TODO: record it, label it as recorded).
- If the dots stay red: the simulator is not running, start it, or open stops yourself on the laptop. Only grains that are activated move, idle ones are activated on v2 when called next.
- If v2 does not start: skip steps 2 to 3, explain step 4 with the slide and stop at "what would happen".

---

<!-- HIIIIIIIIIIIER STARTEN DIE GENERIERTEN SLIDES -->

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

## Timers vs reminders<sup class="ref">1</sup>

| | Timer | Reminder |
|---|---|---|
| Lives | In memory | Durable (Postgres) |
| Survives silo crash | No | Yes |
| Use for | Frequent, fine to lose | Must happen anyway |
| In gleis-grain | Faster refresh while watched | Refresh every minute, resolve bets |

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/timers-and-reminders/">https://dotnet.github.io/orleans/docs/grains/timers-and-reminders/</a></div>

Note:
- Speaker: Lucas | Raffi

Rule of thumb: if it must happen even after a crash, use a reminder. Real code: `RegisterGrainTimer` and `RegisterOrUpdateReminder` in `StopGrain`, reminders in `PlayerGrain` (bet resolution) and `AlertBoardGrain`. Reminder period is an option per context. Exact timer period: check the options before quoting a number.

---

## One request at a time<sup class="ref">1</sup>

- Every grain has a mailbox, one message at a time
- No locks inside the grain
- The price: a slow call blocks the next ones
- Deadlock: A waits for B, B waits for A, timeout

<img src="./images/spiderman_deadlock.webp">

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/request-scheduling/">https://dotnet.github.io/orleans/docs/grains/request-scheduling/</a></div>

Note:
- Speaker: Lucas | Raffi

Planned: the Trip <-> Stop deadlock demo is not built, treat it as a whiteboard story. Options: `[ReadOnly]` for queries, `[AlwaysInterleave]` for small independent methods, `AllowCallChainReentrancy()` for a known cycle, `[Reentrant]` only for grains with almost no state. Best fix: break the cycle by design. Reentrancy only changes scheduling inside one activation, it never creates a second one.

---

## The hot grain: Hauptbahnhof

- Everybody picks the same stop
- One activation, one mailbox, one thread
- Fix: a `[StatelessWorker]` board cache<sup class="ref">1</sup>
- Many local copies, read from a snapshot

```csharp
[StatelessWorker, GrainType("stop-board-cache")]
public class StopBoardCacheGrain : Grain, IStopBoardCacheGrain
```

<!-- TODO IMAGE: chart, watchers per stop with Hauptbahnhof as the tall bar -->

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/stateless-worker-grains/">https://dotnet.github.io/orleans/docs/grains/stateless-worker-grains/</a></div>

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

## Cluster: membership<sup class="ref">1</sup>

- Silos watch each other and vote a dead silo out
- Membership lives in the shared `orleans` Postgres database
- Dead silo: its grains are activated again on another silo
- Reminders still fire

```sql
SELECT * FROM orleansmembershiptable;
```

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/implementation/cluster-management/">https://dotnet.github.io/orleans/docs/implementation/cluster-management/</a></div>

Note:
- Speaker: Lucas | Raffi

In pgAdmin on the big screen, the audience sees the dead silo flip. Details in how-orleans-works.md (membership, when a silo dies). Postgres is one choice, Redis or Azure Table are other usual ones.

---

## Cluster: directory and placement

- Directory: distributed, in memory, split over all silos by hash ranges<sup class="ref">1</sup>
- Normally one activation per grain
- Placement: where does a new activation go?<sup class="ref">2</sup>
- Default since Orleans 9.2: resource optimized (before: random)

<!-- TODO IMAGE: hash ring diagram (Mermaid in how-orleans-works.md, "Who knows where a grain lives?") -->

<div class="refs">
  <div><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/host/grain-directory/">https://dotnet.github.io/orleans/docs/host/grain-directory/</a></div>
  <div><sup class="ref">2</sup> <a href="https://dotnet.github.io/orleans/docs/grains/grain-placement/">https://dotnet.github.io/orleans/docs/grains/grain-placement/</a></div>
</div>

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

## Kubernetes<sup class="ref">1</sup>

- `UseKubernetesHosting()` from `Microsoft.Orleans.Hosting.Kubernetes`
- Pod labels `orleans/serviceId`, `orleans/clusterId`
- Downward API env vars, RBAC, probes, long grace period
- Load test with NBomber on replayed XML

<div class="refs"><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/deployment/kubernetes/">https://dotnet.github.io/orleans/docs/deployment/kubernetes/</a></div>

Note:
- Speaker: Raffi

---

## Testing

- Domain classes: plain unit tests, no silo
- Grains: in-process `TestCluster`<sup class="ref">1</sup>
- Storage: in-memory in tests
- Postgres: Testcontainers (skips without Docker)<sup class="ref">2</sup>

<div class="refs">
  <div><sup class="ref">1</sup> <a href="https://dotnet.github.io/orleans/docs/grains/testing/">https://dotnet.github.io/orleans/docs/grains/testing/</a></div>
  <div><sup class="ref">2</sup> <a href="https://dotnet.testcontainers.org/">https://dotnet.testcontainers.org/</a></div>
</div>

Note:
- Speaker: Raffi

---

## Take this home & try it out

- A virtual actor is an identity, memory and activations come and go
- Timers for the cheap, reminders for the important
- Hot grains need a design, not just more silos
- Persist less, version early
- Keep domain code free of Orleans, the grain stays thin

Note:
- Speaker: Lucas

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
