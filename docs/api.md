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
  "watchers": 12, "refreshedAt": "2026-11-17T10:00:00+00:00",
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

## Cluster

| Method | Path | Answer |
|---|---|---|
| GET | `/api/cluster/silos` | `[{ name, address, status }]`, from Orleans membership. Dead silos show up with their status. |
| GET | `/api/cluster/activations` | Active stop grains: `[{ stopId, silo }]`. Lets the map start in the right state. |
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
