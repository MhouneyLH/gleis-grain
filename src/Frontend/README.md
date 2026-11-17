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
| `/` | phones | Nickname once (localStorage), map with all stops, bottom sheet with the board, watch a stop, bet on a departure, own score, top 5 |
| `/projector` | the wall | Grain view on a dark map, leaderboard, silos, watchers per stop, stats, live ticker, chaos button. The navigation is hidden here |
| `/stop/:id` | anyone | Large departure board, refreshed every 15 s, plus the notices of the stop |
| `/alerts` | anyone | Service notices, filter by line and stop |

The projector shows the audience address as big text. Behind a laptop `localhost` is useless, so open `/projector?join=192.168.1.20:5080` to show another address.

## Layout

```
src/
  api/          typed docs/api.md and the one fetch wrapper (problem details become ApiError)
  live/         pure state logic (grain view, ticker, leaderboard, silo colours, formatting), unit tested
  composables/  EventSource wrapper, polling, player, stops, toasts
  map/          Leaflet base map (tiles, resize, labels)
  components/   map and panel components
  views/        one file per page
```

`live/` has no Vue or Leaflet imports. The event stream (`useEventStream`) has one listener per event type; after every reconnect the projector reloads activations, silos and the leaderboard.
