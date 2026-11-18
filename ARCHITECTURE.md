# Architecture

Mermaid source. In Excalidraw use "Insert > Mermaid to Excalidraw" and paste the diagram block.
Everything in the diagram is built. For how to work on the code see [CONTRIBUTING.md](CONTRIBUTING.md), for the HTTP API Scalar on the running host (http://localhost:5080/scalar) and the `.http` files in `src/Host/http`, for agents [AGENTS.md](AGENTS.md).

```mermaid
flowchart LR
    browser([Browser: projector and phone])
    efa[(Linz AG EFA server<br/>XML over HTTP)]
    replay[(Recorded XML responses<br/>replay feed)]
    osm[(OpenStreetMap tiles)]

    subgraph apphost [Aspire AppHost]
        direction TB
        vite[Vite dev server<br/>Vue 3 frontend]
        gateway[Gateway YARP<br/>http :5080]
        sim[Simulator<br/>80 virtual people, control API :5090]
    end

    browser -- "app assets, dev only" --> vite
    vite -- "proxy /api" --> gateway
    vite -- "proxy /sim" --> sim
    browser -- "HTTP and SSE /api" --> gateway
    browser -- "map tiles" --> osm
    sim -- "public HTTP API" --> gateway

    subgraph hosts [GleisGrain.Host: one Orleans cluster]
        direction TB
        subgraph v1 [v1 build: 3 replicas, one silo each]
            direction LR
            h1[Host v1-xxxxx]
        end
        subgraph v2 [v2 build: 3 replicas, started on demand]
            direction LR
            h2[Host v2-xxxxx]
        end
        subgraph modules [Modules in every replica]
            direction LR
            stops[Stops<br/>StopGrain<br/>StopBoardCacheGrain]
            trips[Trips<br/>TripGrain]
            alerts[Alerts<br/>AlertBoardGrain]
            ingestion[Ingestion<br/>SourceGrain<br/>rate limiter, cache, XML parser]
            game[Game<br/>PlayerGrain<br/>LeaderboardGrain]
            cluster[Cluster<br/>event hub grain<br/>silos, stats, chaos]
        end
        common[Common: HTTP, Orleans, OpenTelemetry]
    end

    gateway --> hosts

    stops -- "via Contracts: reminder 1 min,<br/>timer while watched" --> ingestion
    alerts -- "via Contracts: reminder" --> ingestion
    stops -- "OneWay reports" --> trips
    game -- "via Contracts" --> trips
    game -- "via Contracts" --> stops
    stops -- "publish events" --> cluster
    game -- "publish events" --> cluster
    cluster -- "observer per silo,<br/>SSE to the browser" --> browser
    ingestion -- "departure monitor,<br/>notices" --> efa
    ingestion -- "Mode=Replay" --> replay

    subgraph pg [Postgres, one container, schema from db/]
        orleansdb[(orleans<br/>membership + reminders)]
        d1[(stops)]
        d2[(trips)]
        d3[(alerts)]
        d4[(ingestion)]
        d5[(game)]
    end

    hosts --> orleansdb
    stops --> d1
    trips --> d2
    alerts --> d3
    ingestion --> d4
    game --> d5

    apphost_start{{AppHost}} -- starts --> hosts
    apphost_start -- starts --> pg
    apphost_start -- starts --> vite
    hosts -- OTLP --> dashboard[Aspire dashboard]
```

Modules call each other through the `Contracts` projects only. The Cluster module is the hub for live updates: modules publish events to one grain, every silo has one observer on it (subscriptions expire unless renewed), and each replica forwards the events to its browsers as server-sent events, so an event raised on any silo reaches a browser on any replica. Three replicas of the host form one Orleans cluster of identical silos, each with its own silo and gateway port from Aspire. The gateway keeps one stable address in front of them. The chaos button kills the replica that receives the request.

Grain versioning demo: the same host is built twice. v1 (`[Version(1)]` on `IStopGrain`) starts with the AppHost; v2 (built with `-p:GrainVersion=2` into `artifacts/v2`) is started by hand in the Aspire dashboard and joins the same cluster. Silo names are `v<version>-<random>`, so the frontend shows the version as colour and the silo as shape. The cluster uses the BackwardCompatible strategy and the LatestVersion selector. The replicas never build at start, they only run what the AppHost built.

The simulator is a separate small app that acts like virtual people through the public API (join, pick a stop, move, read boards). It has a control API to pin N people to one stop, which creates the hot grain on demand.

The Postgres container loads the Orleans schema from `db/` when it starts on an empty volume. The `orleans` database holds membership and reminders, every module has its own database for its grain state.

Splitting a module into its own deployable later needs only a small host that calls its `Add...Module` and `Map...Endpoints`.
