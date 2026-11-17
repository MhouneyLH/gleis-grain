# Requirements

What gleis-grain should do, from a feature point of view. Every feature exists to show an Orleans idea from the talk.

## Goal
- Live public transport data from Linz AG, shown on a cute map, with Orleans visible underneath.
- A scripted demo for the .NET Stammtisch Linz with audience moments, free play afterwards.
- Discussion matters more than finishing every feature. Must = needed for the demo, Should = nice to have.

## Audience and format
- Audience joins on their phones through a QR code, with a **nickname** only (no accounts, no personal data).
- Presenter drives the demo from one laptop on a projector.
- After the script, anyone can keep playing.

## Data
- Source: Linz AG EFA server, XML over HTTP. Live only.
- Curated set of about 20-30 central stops, not the whole network.
- Polling stays polite: one shared rate limit, results cached in the grains.
- The departure monitor request is available. Service notices come in every response.
- Open: terms of use and rate limits.

## Features

### F1 Departure board (Must)
- Show the next departures of a stop, with delay.
- Data comes from the stop's grain, not from a fresh request per viewer.
- Refreshes about every minute (reminder), faster while somebody watches (timer).

### F2 Service notices (Must)
- List current notices, filterable by line or stop.
- Refreshed by a reminder in the background.

### F3 Map of Linz (Must)
- A Leaflet map with public OpenStreetMap tiles, on the projector and on the phone. Stops are dots on it. Readable on a projector.
- Click a stop to open its board.
- **Grain view:** a stop is lit while its grain is active and fades when deactivated. Dot colour shows the silo that hosts it. When a silo dies, its dots flash and change colour.

### F4 Pick your stop (Must)
- Each person picks one stop and becomes a watcher of it.
- Live count of watchers per stop on the map and as a bar chart.
- Expected result: one hot stop (Hauptbahnhof). A fix is shown afterwards (cached snapshot or `[StatelessWorker]`).

### F5 Guess the delay (Must)
- A person bets on whether the next departure at a stop will be late, and by how much: the guess is the delay in minutes, 0 to 15.
- Scoring: exact guess 3 points, off by one minute 1 point, otherwise 0. One open bet per player at a time.
- A **reminder** resolves each bet when the departure time has passed. Live leaderboard by nickname.
- Bets must survive a silo restart.

### F6 Chaos button (Must)
- Presenter (or an audience vote) kills one silo while the game and the map keep running.
- The button kills the replica that receives the request. It only works when `Cluster:EnableChaos` is true, which the AppHost sets for the demo.
- Grains come back on the other silos, bets still resolve, the map shows it.
- Needs at least 3 host replicas, each with its own silo and gateway port. The AppHost starts three and Aspire assigns the ports (endpoints per replica).

### F7 Live updates (Must)
- The browser receives changes through **server-sent events**: stop activation, watchers, bets, leaderboard.
- An event raised on any silo must reach a browser connected to any replica. Events travel through an Orleans grain observer hub (module Cluster): every silo subscribes one observer, and subscriptions expire unless renewed, so a dead silo drops out by itself. Not Orleans streams.

### F8 Crowd reports (Later)
- "Tram is full" or "stop closed" with votes. One grain per report.
- Not part of the first version.

### F9 Stats panel (Should, in)
- Cache hits versus calls to Linz AG, active grains, hottest stops.
- Part of the first version.

## Tools in the demo
- **Aspire dashboard:** resources, logs, traces, metrics.
- **Orleans Dashboard:** silos, grains, activations.
- **Scalar:** try the API live.
- **pgAdmin:** `SELECT * FROM orleansmembershiptable;`

## Demo script
| Step | Concept | Feature | Audience action |
|---|---|---|---|
| 1 | Virtual actors | F3 | Watch stops light up and fade as people open them |
| 2 | State | F1 | Open a board, then restart a silo: state is still there |
| 3 | Timers vs reminders | F1, F5 | Place a delay bet; show the timer while watched, the reminder otherwise |
| 4 | Hot grain, concurrency | F4 | Everyone picks a stop; Hauptbahnhof gets hot; show the fix |
| 5 | Cluster, membership | F6, F7 | Vote to kill a silo; see membership flip and dots change colour |
| 6 | Versioning (optional) | F1 | Roll out v2 with an extra field while play continues |
| 7 | Free play | F4, F5, F9 | Everyone plays; discussion |

## Non-functional
- **Stage safety:** the Linz AG server being down must not kill the demo. Recorded XML responses can be replayed (also needed for tests and CI).
- **Politeness:** never more than the agreed request rate to Linz AG, however many people join.
- **Scale target:** about 100 phones at once, one stop per phone.
- **Phone first:** works on a small screen, no app, no login.
- **Privacy:** nicknames only, state of the game is deleted after the event.
- **Offline-friendly:** the frontend assets (app, scripts, styles) come from the host itself, no CDN. The map tiles come from public OpenStreetMap servers and need internet.

## Frontend
- Vue 3 with Vite, so there is a build step. The build goes into the host's `wwwroot`, and the host serves it: audience page, projector page (map and leaderboard), departure board page.
- During development the AppHost starts the Vite dev server (hot reload, proxies `/api` to the host).
- Server-sent events for live data.

## Out of scope
- Journey planning, tickets, accounts, real vehicle positions.
- Full Linz network, other cities.
- Authentication, Keycloak.

## Decisions
- The departure monitor request is available.
- Nickname: trimmed, 2 to 20 characters, unique ignoring case. A duplicate answers 409. No profanity filter.
- Events travel through an Orleans grain observer hub with expiring subscriptions, not through streams.
- Every replica gets its silo and gateway port from Aspire endpoints.
- Bets: guess the delay in minutes (0 to 15), exact 3 points, off by one 1 point, one open bet per player.
- The chaos button kills the replica that receives the request.

## Open questions
- Linz AG terms of use and rate limits.
