# AGENTS.md

Important facts for AI coding agents working in this repo. Humans: start with [README.md](README.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

## What this is

gleis-grain is the demo for a talk about Microsoft Orleans (.NET Stammtisch Linz). Linz AG stops, trips and service notices are virtual actors. It is a showcase: **readable, simple code beats clever code**. Orleans 10.4, .NET 10, Aspire 13.6, Postgres, a Vue 3 frontend and a reveal.js slide deck.

Where to look:

| What | Where |
|---|---|
| How the parts fit (diagram) | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Rules, workflow, commit style, tests | [CONTRIBUTING.md](CONTRIBUTING.md) |
| HTTP API | Scalar on the running host (http://localhost:5080/scalar, OpenAPI at `/openapi/v1.json`) |
| Live event payloads | `src/Features/Cluster/GleisGrain.Cluster.Contracts/LivePayloads.cs` |
| Try the API | `src/Host/http/*.http`, `src/Simulator/http/*.http` |
| The talk (slides, diagram sources, notes) | `docs/net_stammtisch_linz_presentation/` |

## Layout

`src/Host` is the one deployable (modular monolith). Six modules live in `src/Features/<Module>/`: Stops, Trips, Alerts, Ingestion, Game, Cluster; each is `GleisGrain.<Module>` plus `GleisGrain.<Module>.Contracts`. `src/Common` is shared setup, `src/AppHost` starts everything, `src/Simulator` simulates the audience, `src/Frontend` is the Vue app, `db/` holds the Orleans SQL schema, `test/` mirrors `src`.

## Commands

```bash
dotnet build gleis-grain.slnx -nodeReuse:false      # 0 warnings required, warnings are errors
dotnet test --solution gleis-grain.slnx --no-build   # all tests (the Postgres one needs Docker)
dotnet run --project src/AppHost --launch-profile replay   # whole demo; "live" asks Linz AG
cd src/Frontend && npm ci && npm run type-check && npm test && npm run build
cd docs/net_stammtisch_linz_presentation/slides && npm install && npm run build
```

## Rules that are enforced

- **Strict build.** Analyzers on, style from `.editorconfig`, warnings are errors. Fix the cause, do not suppress.
- **Module boundaries** (tests in `test/Modules` fail otherwise): a module exposes only its `<Module>Module` class, everything else is `internal`; modules talk only through each other's Contracts; Contracts reference no module, no `Common`, no ASP.NET Core.
- **Domain code is plain C#**: no Orleans, no I/O, time through `TimeProvider`. A grain is a thin wrapper that loads state, calls the domain class, persists and publishes.
- **Persisted state and serialized types** are an API: `[GenerateSerializer]`, `[Alias]`, stable `[Id(n)]`, only add fields with new ids, never reuse or retype an id; state carries a `SchemaVersion`.
- Strongly typed ids (`StopId`, `TripId`, `Nickname`...) in the owning module's Contracts. Expected failures are `DomainError` records thrown as `DomainException` in endpoint handlers; never throw them across a grain call.
- Logging with `[LoggerMessage]` and named placeholders (EventId ranges: Stops 1000, Trips 2000, Alerts 3000, Ingestion 4000, Game 5000, Cluster 6000). Metrics through `IMeterFactory`; a module's meter name must be registered in `src/Host/Program.cs`. No personal data in logs. Players are nicknames only.
- Packages are pinned centrally in `Directory.Packages.props`; run `dotnet restore` and commit the `packages.lock.json` files after a package change (CI restores in locked mode).
- Tests: xunit.v3, name `Method_Condition_Result`, no real time (`FakeTimeProvider`), **Linz AG is never called from a test or CI**, use the recorded replay data. If an endpoint or live event changes, update the matching `.http` file and the frontend (`src/Frontend/src/api`) in the same change.

## Pitfalls we hit (read before debugging)

- **One folder, two environments.** The dev container (user `vscode`) and the host (another user) share the repo folder, each with its own NuGet cache. Each restore rewrites `obj/project.assets.json`, so the other side fails with "Package ... was not found" or "Orleans types missing". Build and restore from one environment at a time; `dotnet restore gleis-grain.slnx --force` repairs it. VS Code's C# extension also restores in the background; wait for it. Use `-nodeReuse:false`. Do not run `dotnet test` with that flag.
- **Postgres "password authentication failed".** The data volume `gleis-grain-postgres` keeps its first password. The AppHost now uses the fixed dev password `gleis-grain-dev`; if the volume is older, stop the AppHost and `docker volume rm gleis-grain-postgres` (demo data only).
- **Host replicas never build at start.** The AppHost starts v1 and v2 with `--no-build`. v2 is built once into `artifacts/v2` by an Exec step in `GleisGrain.AppHost.csproj` (`-p:SkipGrainV2Build=true` skips it). Never switch back to `AddProject(name, path)` with replicas, they race on the build and some replicas exit ("Finished").
- **Grain versioning demo.** `IStopGrain` is `[Version(1)]`, or `[Version(2)]` with `-p:GrainVersion=2`. Silo names are `v<version>-<random>`; the frontend derives version (colour) and slot (shape) from them. v2 only adds.
- **Fake clocks and Orleans test silos.** Do not register a frozen `FakeTimeProvider` as the silo's global `TimeProvider`: Orleans uses it for membership and the silo declares itself dead. Use a clock that is the system clock plus an offset (`ShiftableClock` in the Alerts and Ingestion tests), and a unique ClusterId/ServiceId per test host.
- **Replay feed** is a loop on a fixed clock (departures really leave, trip ids stay stable). Recordings are real Linz AG answers; re-record in daytime with `src/Features/Ingestion/GleisGrain.Ingestion/Replay/record.sh`. Check the Linz AG terms of use before making the repo public.
- **Orleans reminders** cannot be shorter than 1 minute by default.
- **The simulator control API and the `/sim` proxy** work with the Vite dev server only, not with the built `wwwroot`.

## Slides (`docs/net_stammtisch_linz_presentation/slides`)

- Content is one file, `slides.md` (reveal.js markdown). Speakers edit it by hand: keep their wording and titles. Everything before the comment `<!-- HIIIIIIIIIIIER STARTEN DIE GENERIERTEN SLIDES -->` is hand-written, new slides go before it unless told otherwise.
- Each slide's note starts with a `- Speaker: ...` line. No employer name and no "dispatch idea" origin story anywhere in the slides.
- References: write `<sup class="ref">1</sup>` and a `<div class="refs">` with the plain URL; `src/main.ts` numbers them across the whole deck and fills the closing "References" slides. Only link pages you fetched and checked (HTTP 200 and the right topic).
- Diagrams: Excalidraw sources in `excalidraw/` with the generator `generate.py`, rendered PNGs in `slides/public/images/` (as-designed colours, dark background; do not use Excalidraw's dark-mode export, it inverts these diagrams). Mermaid sources are in `concurrency-diagrams.md`.
- Check changes visually: build, `npx vite preview`, screenshot with headless chromium, look at the image.

## Working agreements

- **Commits**: Conventional Commits with the context as scope (`feat(stops): ...`). Subject line (at most 72 characters) plus the co-author trailer, no long body. One topic per commit; images in their own commit.
- **Do not push, force-push or rewrite pushed history unless the owner asks.** Do not delete volumes, containers or branches you did not create.
- Several agents may work in parallel in git worktrees: stay inside the paths you were given and report anything outside them instead of editing it.
- Leave other people's uncommitted edits alone (the speakers edit `slides.md` and `diagrams.excalidraw` while you work). Read the file again before you change it.
