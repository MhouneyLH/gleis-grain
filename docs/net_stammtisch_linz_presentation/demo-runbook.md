# Demo runbook

## Before the talk

| # | Do | Check |
|---|---|---|
| 1 | Stop an old AppHost (`aspire stop` or Ctrl+C) | no old containers running |
| 2 | Only if Postgres is unhealthy or "password authentication failed": `docker volume rm gleis-grain-postgres` | demo data only |
| 3 | `dotnet run --project src/AppHost --launch-profile replay` (`--launch-profile live` if the network is good) | wait until the dashboard http://localhost:15000 is green |
| 4 | Open the `Frontend` link in the Aspire dashboard (port changes at every start), go to **Overview** (`/overview`), full screen | map with dots |
| 5 | http://localhost:5080/api/stops | lists 25 stops |
| 6 | Dashboard resources | `gleis-grain-v1-*` x3 running, `gleis-grain-v2-*` x3 NotStarted, `simulator` NotStarted |

Tabs: Aspire dashboard (15000), Frontend (Overview), http://localhost:5080/api/cluster/silos, Scalar http://localhost:5080/scalar, `src/Simulator/http/simulator.http`.

## Demo: Showcase

| # | Click | Say | Expected |
|---|---|---|---|
| 1 | Overview page | "Every dot is a stop, every stop is a grain." | grey/dark dots |
| 2 | Dashboard: start `simulator` | "80 simulated people join over about 40 seconds." | ~40 s ramp, dots light up |
| 3 | Point at Hauptbahnhof | "One stop gets hot." | biggest dot (about 40 % of people) |
| 4 | Simulator panel on Overview, or `PUT :5090/scenario/pinned/60501720` `{ "people": 50 }` (100 for more) | "50 more people watch one stop." | dot grows |
| 5 | Point at shapes and size | "Shape = silo, size = watchers." | 3 shapes |
| 6 | Optional: chaos button | "One silo dies, the grains move." | shapes change, app keeps answering |

Release the crowd: `DELETE :5090/scenario/pinned`, or pin 0.

## Demo: Versioning

1. Overview: all dots red, three shapes (v1).
2. Dashboard: start the three `gleis-grain-v2-*` replicas, one click each.
3. http://localhost:5080/api/cluster/silos: six Active silos. Say: "One cluster, two versions."
4. Dashboard: stop the three `gleis-grain-v1-*` replicas one by one (abrupt kill, a request may hang briefly).
5. Dots turn blue, the app keeps answering. Say: "A rolling update without a maintenance window."

### What differs between v1 and v2 (for questions)

Only the `IStopGrain` interface version. The build switch is `-p:GrainVersion=2` (compile constant `GRAIN_V2`).

| What | v1 | v2 | Code |
|---|---|---|---|
| Interface version | `[Version(1)]` | `[Version(2)]` | [IStopGrain.cs L8-12](../../src/Features/Stops/GleisGrain.Stops.Contracts/IStopGrain.cs#L8-L12) |
| New method | none | `GetGrainVersionAsync()` | [IStopGrain.cs L27-31](../../src/Features/Stops/GleisGrain.Stops.Contracts/IStopGrain.cs#L27-L31), implemented in [StopGrain.cs L76-79](../../src/Features/Stops/GleisGrain.Stops/Domain/StopGrain.cs#L76-L79) |
| Version constant | `Current = 1` | `Current = 2` | [StopsGrainVersion.cs L9-13](../../src/Features/Stops/GleisGrain.Stops.Contracts/StopsGrainVersion.cs#L9-L13) |
| Field on the board | `grainVersion: 1` | `grainVersion: 2` | [StopBoardSnapshot.cs L15-17](../../src/Features/Stops/GleisGrain.Stops.Contracts/StopBoardSnapshot.cs#L15-L17) (new `[Id(7)]`, in both builds) |
| Where the switch is set | | | [Stops.Contracts.csproj](../../src/Features/Stops/GleisGrain.Stops.Contracts/GleisGrain.Stops.Contracts.csproj), [AppHost.csproj L26](../../src/AppHost/GleisGrain.AppHost.csproj#L26), [AppHost Program.cs L62](../../src/AppHost/Program.cs#L62) |

Same in both builds: silo name `v<version>-<random>` and the BackwardCompatible strategy with the LatestVersion selector ([OrleansExtensions.cs L58-69](../../src/Common/OrleansExtensions.cs#L58-L69)).

- **State migrated?** No. `StopState` is identical in both builds, there is no `SchemaVersion` migration (planned, not built). The only data change is the additive `[Id(7)]` field.
- **Grain migrated?** No live migration. A stopped v1 silo takes its activations with it; the next call creates a new activation on a v2 silo, which reads the same state from Postgres. That re-activation is the red-to-blue change on the map.

## Fallbacks

| Problem | Fix |
|---|---|
| No network or Linz AG slow | restart with `--launch-profile replay` |
| Simulator does not react | restart it in the dashboard, check `GET :5090/scenario` |
| Replica shows `Finished` | click start on it |
| Postgres "password authentication failed" | stop AppHost, `docker volume rm gleis-grain-postgres`, start again |
| Stops missing on the map | reload the page |
| Frontend port unknown | `Frontend` link in the dashboard; gateway is always :5080, simulator API :5090 |
| Everything down | recorded video |

## After the talk

`aspire stop` or Ctrl+C in the AppHost terminal.
