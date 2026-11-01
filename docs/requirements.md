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
- Open: terms of use, rate limits, and whether the departure monitor request is available.

## Features

### F1 Departure board (Must)
- Show the next departures of a stop, with delay.
- Data comes from the stop's grain, not from a fresh request per viewer.
- Refreshes about every minute (reminder), faster while somebody watches (timer).

### F2 Service notices (Must)
- List current notices, filterable by line or stop.
- Refreshed by a reminder in the background.

### F3 Schematic map of Linz (Must)
- Stylised SVG, tram lines as coloured lines, stops as dots. Works offline, readable on a projector.
- Click a stop to open its board.
- **Grain view:** a stop is lit while its grain is active and fades when deactivated. Dot colour shows the silo that hosts it. When a silo dies, its dots flash and change colour.

### F4 Pick your stop (Must)
- Each person picks one stop and becomes a watcher of it.
- Live count of watchers per stop on the map and as a bar chart.
- Expected result: one hot stop (Hauptbahnhof). A fix is shown afterwards (cached snapshot or `[StatelessWorker]`).

### F5 Guess the delay (Must)
- A person bets on whether the next departure at a stop will be late, and by how much.
- A **reminder** resolves each bet when the departure time has passed. Live leaderboard by nickname.
- Bets must survive a silo restart.

### F6 Chaos button (Must)
- Presenter (or an audience vote) kills one silo while the game and the map keep running.
- Grains come back on the other silos, bets still resolve, the map shows it.
- Needs at least 3 host replicas, each with its own silo and gateway port.

### F7 Live updates (Must)
- The browser receives changes through **server-sent events**: stop activation, watchers, bets, leaderboard.
- An event raised on any silo must reach a browser connected to any replica.

### F8 Crowd reports (Should)
- "Tram is full" or "stop closed" with votes. One grain per report.

### F9 Stats panel (Should)
- Cache hits versus calls to Linz AG, active grains, hottest stops.

## Tools in the demo
- **Aspire dashboard:** resources, logs, traces, metrics.
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
| 7 | Free play | F4, F5, F8 | Everyone plays; discussion |

## Non-functional
- **Stage safety:** the Linz AG server being down must not kill the demo. Recorded XML responses can be replayed (also needed for tests and CI).
- **Politeness:** never more than the agreed request rate to Linz AG, however many people join.
- **Scale target:** about 100 phones at once, one stop per phone.
- **Phone first:** works on a small screen, no app, no login.
- **Privacy:** nicknames only, state of the game is deleted after the event.
- **Offline-friendly:** the map and the frontend assets come from the host itself, no CDN.

## Frontend
- Static pages served by the host, no build step: audience page, projector page (map and leaderboard), departure board page.
- Server-sent events for live data.

## Out of scope
- Journey planning, tickets, accounts, real vehicle positions.
- Full Linz network, other cities.
- Authentication, Keycloak.

## Open questions
- Linz AG terms of use and rate limits.
- Is the departure monitor request available, and what does it return?
- Nickname moderation (profanity, duplicates).
- How events travel between silos for the event stream (Orleans streams or observers).
- Running several replicas locally needs a port per replica.
