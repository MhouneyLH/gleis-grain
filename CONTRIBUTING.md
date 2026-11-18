# Contributing

gleis-grain is the demo for a talk about Microsoft Orleans, so it has to stay easy to read. Small, clear changes are worth more than clever ones. [README.md](README.md) explains what the project is, this file how to work on it, and [AGENTS.md](AGENTS.md) holds the rules the build and the tests enforce, plus the pitfalls we already hit. Read it before your first change.

## Setup

Everything runs in the devcontainer (VS Code with the Dev Containers extension and Docker on the host, then "Reopen in Container"). Without it you need the .NET SDK from `global.json`, Node 22.12 or newer and Docker.

| Task | Command |
|---|---|
| Build | `dotnet build gleis-grain.slnx` |
| Test everything | `dotnet test --solution gleis-grain.slnx` |
| Test one project | `dotnet test --project test/Features/Stops/GleisGrain.Stops.Tests` |
| Run the whole demo | `dotnet run --project src/AppHost` (`--launch-profile replay` for recorded data) |
| Try the API | the `.http` files in `src/Host/http` and `src/Simulator/http` |
| Frontend | in `src/Frontend`: `npm ci`, `npm run dev`, `npm test`, `npm run build` |

Only `test/Infrastructure/GleisGrain.Postgres.Tests` needs Docker; it skips itself without it.

## Running the demo

`dotnet run --project src/AppHost` starts everything; the Aspire dashboard on http://localhost:15000 lists the resources:

- **postgres, pgadmin:** one database per module plus `orleans`, schema from `db/` (loaded only on an empty volume). Login errors or a changed schema: stop the AppHost and `docker volume rm gleis-grain-postgres`.
- **gleis-grain:** the gateway on http://localhost:5080 in front of **gleis-grain-v1** (three replicas, started) and **gleis-grain-v2** (three replicas, start them by hand). `/dashboard` is the Orleans Dashboard, `/scalar` the API docs.
- **simulator:** virtual audience, start it in the dashboard. Options `Simulator:People`, `RampUpSeconds`, `HotStopShare`, `Seed`; control API on :5090 ([docs/api.md](docs/api.md#simulator-control-api)).
- **frontend:** the Vite dev server (it proxies `/api` and `/sim`).

Rollout demo: start the three `gleis-grain-v2-...` replicas, wait until `/api/cluster/silos` lists six Active silos, then stop the three `gleis-grain-v1-...` replicas. By hand, v2 is built with `dotnet build src/Host -p:GrainVersion=2 --artifacts-path artifacts/v2`; the AppHost does that after each build. To look at the cluster run `SELECT * FROM orleansmembershiptable;` in pgAdmin (filter on the newest `deploymentid`). Details and pitfalls (shared folder, passwords, no builds at start): [AGENTS.md](AGENTS.md).

## Workflow

1. Open an issue first for anything bigger than a small fix, so we can agree on the idea. Use the issue templates.
2. Branch from `main`. Name the branch after the change: `feat/stops-board-cache`, `fix/game-leaderboard-order`.
3. Keep a pull request to one bounded context where you can. A change in `Common` or the build files runs every module's CI, so keep those separate.
4. Fill in the pull request template. CI must be green and the build free of warnings.

CI only runs the modules a change touches, plus the boundary tests in `test/Modules`. A change to `Common`, the build props or the workflows runs all of them. The simulator job runs when `src/Simulator` or `test/Simulator` changes. The frontend job runs when `src/Frontend` changes, the Postgres job when `db/` or `test/Infrastructure` changes.

## Commit messages

[Conventional Commits](https://www.conventionalcommits.org/), with the context as scope:

```
feat(stops): refresh the board while somebody watches
fix(game): keep the highest score when reports arrive out of order
docs(readme): explain the Orleans Dashboard link
```

- Types: `feat`, `fix`, `refactor`, `test`, `docs`, `build`, `ci`, `chore`.
- Scopes: `stops`, `trips`, `alerts`, `ingestion`, `game`, `cluster`, `frontend`, `apphost`, `common`, plus `readme`, `requirements`, `talk` for docs.
- The subject is imperative and at most 72 characters. The body explains why, not what.
- A commit does one thing and leaves the build and the tests green.

The build is strict (warnings are errors) and the module boundaries are enforced by tests: see [AGENTS.md](AGENTS.md).

## Adding a module

1. A project `GleisGrain.<Module>` and `GleisGrain.<Module>.Contracts` under `src/Features/<Module>/`, a test project under `test/Features/<Module>/`, all added to `gleis-grain.slnx`.
2. A `<Module>Module` class with `Add<Module>Module` and `Map<Module>Endpoints`, plus `StorageName` and `MeterName` consts.
3. In `src/Host/Program.cs`: one `Add...Module` call, one `Map...Endpoints` call, its storage name and its meter name.
4. A database for it in `src/AppHost/Program.cs`, its path filter in `.github/workflows/ci.yml`, and an entry in `ModuleBoundaryTests`.

## Tests

Domain unit tests need no silo (most tests); grain tests use an in-process `TestCluster` with in-memory storage and `FakeTimeProvider`; one Testcontainers test checks the real Orleans schema; the frontend uses Vitest. Name tests `Method_Condition_Result`. Linz AG is never called from a test, use the recorded replay data. If an endpoint or live event changes, update [docs/api.md](docs/api.md), the matching `.http` file and the frontend in the same pull request.

## Releases

Releases are cut by pushing a tag on `main`. The release workflow checks the tag, builds and tests with that version and creates a GitHub release with generated notes.

- `1.2.3` is a release.
- `1.2.3-rc.1` is a release candidate and is marked as pre-release.

```
git tag 1.2.3-rc.1
git push origin 1.2.3-rc.1
```

## License

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE) of this project.
