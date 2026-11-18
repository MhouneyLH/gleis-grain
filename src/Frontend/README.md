# Frontend

Vue 3 + TypeScript + Vite + vue-router + Leaflet. No UI framework, no chart library, no CDN: everything is bundled. The only external request is the OpenStreetMap tile layer; when tiles do not load, the stops are still drawn on a plain background.

The contract with the host is [docs/api.md](../../docs/api.md). All calls and types are in `src/api`.

## Run it

Needs Node 22.12 or newer.

```
npm ci
npm run dev
```

The dev server proxies `/api` (including the SSE stream, unbuffered) to the host. The target is `GLEIS_GRAIN_HTTP` (set by Aspire) and falls back to `http://localhost:5080`.

It also proxies `/sim` to the audience simulator (control API, the `/sim` prefix is removed). The target is `SIMULATOR_HTTP` and falls back to `http://localhost:5090`.

## Scripts

| Script | What it does |
|---|---|
| `npm run dev` | Vite dev server |
| `npm run type-check` | `vue-tsc --noEmit` |
| `npm run test` | `vitest run` |
| `npm run build` | type check, then `vite build` into `../Host/wwwroot` (git-ignored, emptied on every build) |

The host serves the built files with an SPA fallback, because the router uses history mode.

## Pages

| Route | For | What |
|---|---|---|
| `/` | phones | Full screen map with all stops, a guest name (`Guest-` plus 4 characters) is created automatically on first load, stored in localStorage and claimed with `POST /api/game/players` (retry on 409, no prompt), tap a stop for its board (with a `grain v1/v2` chip when the API sends `grainVersion`), watch a stop. Bets, score and leaderboard only with `BETTING_ENABLED` |
| `/projector` | the wall | Full screen grain map (no page scroll) with floating translucent panels in the two side columns: join address, legend, simulation, chaos button, silos (leaderboard only with `BETTING_ENABLED`), watchers per stop, stats, live ticker. Each panel collapses with its header; `H` (or the small button at the bottom) hides or shows all panels. The navigation is hidden here |
| `/stop/:id` | anyone | Large departure board, refreshed every 15 s, plus the notices of the stop |
| `/alerts` | anyone | Service notices, filter by line and stop |

## Simulation panel (projector)

The panel steers the audience simulator (about 80 virtual people). It shows the total people and how many joined, the pinned groups (stop name, people), and polls `GET /sim/scenario` every 5 s while it is open. Click a stop on the map to select it (highlight ring), then press `10`, `25`, `50`, `100` or `0 (release)` to pin that many people to it (`PUT /sim/scenario/pinned/{stopId}`). `Hot station: Hauptbahnhof 100` pins 100 on stop `60501720`, `Release all` calls `DELETE /sim/scenario/pinned`. Pinned stops get a yellow badge with the count on the map. When the simulator does not answer (network error, 502, 503, 504) the panel says to click start on `simulator` in the Aspire dashboard. The calls are in `src/api`, the pure logic (presets, view model, error check) in `src/live/simulation.ts`.

The projector shows the audience address as big text. Behind a laptop `localhost` is useless, so open `/projector?join=192.168.1.20:5080` to show another address.

## Layout

```
src/
  api/          typed docs/api.md and the one fetch wrapper (problem details become ApiError)
  live/         pure state logic (grain view, ticker, leaderboard, silo slots, formatting), unit tested
  composables/  EventSource wrapper, polling, player, stops, toasts
  map/          Leaflet base map (tiles, resize, labels)
  components/   map and panel components
  views/        one file per page
```

`live/` has no Vue or Leaflet imports. The event stream (`useEventStream`) has one listener per event type; after every reconnect the projector reloads activations, silos and the leaderboard.

## Map encoding (projector)

Each active stop is a shape with a white ring: the **shape** is the silo hosting the grain, the **colour** is the grain version (v1 red, v2 blue, further versions amber and mint). Inactive stops are small dim grey circles. The size and the badge show the watchers. A grain that wakes up or changes silo or version flashes. The legend on the map and the silo list repeat shapes and `v1`/`v2` text, so it also works without colour vision.

Silo names are `v<version>-<random>`. The version is the number after the leading `v` (no match means version 1). The slot is the rank among the active silos of the same version, sorted by name: slot 1 square, 2 circle, 3 triangle, 4 and up wrap around. `live/siloSlots.ts` remembers the slot per silo name for the session: slots stay while the set of active silos does not change, a dead silo frees its slot and its replacement reuses it.

## Betting switch

`src/config.ts` has `BETTING_ENABLED = false`. It hides the bet buttons, stepper, open bet, score, leaderboard and bet toasts on the audience page and the leaderboard on the projector. The code and its tests stay; set it to `true` to bring it all back.
