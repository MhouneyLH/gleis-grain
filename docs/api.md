# API

The contract between the host and the frontend. All endpoints live under `/api`. JSON uses camelCase. Ids are plain strings. Errors are RFC 9457 problem details with a `code` extension (see `Common/Errors`).

## Stops

| Method | Path | Answer |
|---|---|---|
| GET | `/api/stops` | The curated stops: `[{ id, name, latitude, longitude }]`. Static, does **not** activate any grain. |
| GET | `/api/stops/{id}` | The board of one stop. Activates the stop grain. 404 `stops.stop.not_found` for an unknown id. |

Board:

```json
{
  "id": "60501720", "name": "Linz/Donau Hbf", "latitude": 48.29, "longitude": 14.29,
  "watchers": 12, "refreshedAt": "2026-11-17T10:00:00+00:00", "grainVersion": 1,
  "departures": [
    { "tripId": "60501720:1:202611171005", "line": "1", "direction": "Universität",
      "plannedAt": "2026-11-17T10:05:00+00:00", "delayMinutes": 2, "platform": "A" }
  ]
}
```

## Alerts

| Method | Path | Answer |
|---|---|---|
| GET | `/api/alerts?line=&stop=` | Current notices, optionally filtered: `[{ id, title, text, lines, stopIds, firstSeenAt }]` |

## Game

| Method | Path | Body | Answer |
|---|---|---|---|
| POST | `/api/game/players` | `{ "nickname": "Max" }` | 201 with the player. 409 `game.nickname.taken`. 400 for an invalid nickname. |
| GET | `/api/game/players/{nickname}` | | The player. 404 `game.player.not_found`. |
| PUT | `/api/game/players/{nickname}/stop` | `{ "stopId": "60501720" }` | 204. 404 for an unknown player or stop. |
| POST | `/api/game/players/{nickname}/bet` | `{ "tripId": "...", "guessedDelayMinutes": 2 }` | 201 with the player. 404 unknown player or trip, 409 `game.bet.already_open`, 409 `game.trip.departed`, 400 for a guess outside 0..15. |
| GET | `/api/game/leaderboard` | | Top 10: `[{ nickname, score }]` |

Player:

```json
{ "nickname": "Max", "score": 4, "watchedStopId": "60501720",
  "openBet": { "tripId": "...", "stopId": "60501720", "line": "1",
               "plannedAt": "2026-11-17T10:05:00+00:00", "guessedDelayMinutes": 2 } }
```

`watchedStopId` and `openBet` are `null` when not set.

`grainVersion` is 1 or 2: the grain interface version of the build whose silo serves the stop grain. It flips from 1 to 2 when the grain moves to a v2 silo (see Grain versioning).

## Cluster

| Method | Path | Answer |
|---|---|---|
| GET | `/api/cluster/silos` | `[{ name, address, status }]`, from Orleans membership. Dead silos show up with their status. `name` follows the silo naming contract below. |
| GET | `/api/cluster/activations` | Active stop grains: `[{ stopId, silo }]`, `silo` is the silo name. Lets the map start in the right state. |
| GET | `/api/cluster/stats` | `{ ingestion: { calls, cacheHits }, activeGrains: { "<grain type>": count } }` |
| POST | `/api/cluster/chaos/kill` | 202, then the replica that got the request exits abruptly. 404 `cluster.chaos.disabled` unless `Cluster:EnableChaos` is true. |
| GET | `/api/cluster/events` | Server-sent events, see below. |

## Live events

`GET /api/cluster/events` is `text/event-stream`. Every event has `event: <type>` and a JSON `data:`. The payload records are in `GleisGrain.Cluster.Contracts/LivePayloads.cs`.

| Event | Data | When |
|---|---|---|
| `stop.activated` | `{ stopId, silo }` | A stop grain woke up (also after a silo died and the grain moved). |
| `stop.deactivated` | `{ stopId }` | A stop grain was deactivated. |
| `stop.watchers` | `{ stopId, watchers }` | Someone joined or left a stop. |
| `bet.placed` | `{ nickname, stopId, line }` | A bet was placed. |
| `bet.resolved` | `{ nickname, tripId, actualDelayMinutes, points }` | A bet was resolved. |
| `leaderboard.changed` | `{ top: [{ nickname, score }] }` | The ranking changed. |

The stream also sends a comment line (`: keep-alive`) every 15 seconds. `EventSource` reconnects by itself, so when a silo dies the browser continues on another replica. After a reconnect the frontend reloads `/api/cluster/activations`.

## Grain versioning

The demo runs two builds of the host in one Orleans cluster: v1 silos and v2 silos. `IStopGrain` has `[Version(1)]` in the v1 build and `[Version(2)]` in the v2 build (`dotnet build -p:GrainVersion=2`). v2 only adds: `StopBoardSnapshot.GrainVersion` (`[Id(7)]`, the `grainVersion` of the board) and the method `GetGrainVersionAsync`. The cluster uses the `BackwardCompatible` strategy and the `LatestVersion` selector, so v1 callers can use v2 grains and a new activation goes to a v2 silo as soon as one is in the cluster.

Silo naming contract: the silo name is `v<grain version>-<5 random hex chars>`, for example `v1-3f9a1` or `v2-b72c0`. `/api/cluster/silos` returns it as `name`, and `stop.activated` events and `/api/cluster/activations` carry the silo **name** (not the address). The frontend reads the grain version from the prefix, and the position of a silo among the silos of the same version (sorted by name) gives its shape. Dead silos stay in `/api/cluster/silos` with status `Dead`, so filter on `Active`.

Rollout: the AppHost starts `gleis-grain-v1`; the presenter starts the `gleis-grain-v2` replicas in the Aspire dashboard, then stops the `gleis-grain-v1` replicas. Stop grains on stopped v1 silos reactivate on v2 silos at the next call, and their `stop.activated` event names a `v2-...` silo. The frontend talks to one stable url (the `gleis-grain` gateway) during the whole rollout.
