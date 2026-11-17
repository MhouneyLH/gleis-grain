# Architecture

Mermaid source. In Excalidraw use "Insert > Mermaid to Excalidraw" and paste the diagram block.
Solid arrows are built, dashed arrows are not.

```mermaid
flowchart LR
    browser([Browser: phone and projector])
    efa[(Linz AG EFA server<br/>XML over HTTP)]
    replay[(Recorded XML responses<br/>replay feed)]
    osm[(OpenStreetMap tiles)]

    subgraph apphost [Aspire AppHost]
        direction TB
        vite[Vite dev server<br/>Vue 3 frontend]
        lb[Load balancer<br/>http :5080]
    end

    browser -- "app assets, dev only" --> vite
    vite -- "proxy /api" --> lb
    browser -- "HTTP and SSE /api" --> lb
    browser -- "map tiles" --> osm

    subgraph replicas [GleisGrain.Host: 3 replicas, one Orleans silo each, one cluster]
        direction TB
        subgraph modules [Modules in every replica]
            direction LR
            stops[Stops<br/>StopGrain]
            trips[Trips<br/>TripGrain]
            alerts[Alerts<br/>AlertGrain]
            ingestion[Ingestion<br/>SourceGrain<br/>rate limiter + XML parser]
            game[Game<br/>PlayerGrain<br/>LeaderboardGrain]
            cluster[Cluster<br/>event hub grain<br/>silos, stats, chaos]
        end
        common[Common: HTTP, Orleans, OpenTelemetry]
        www[wwwroot: built frontend]
    end

    lb --> replicas
    www -. "served in production" .-> browser

    stops -- "via Contracts: reminder 1 min,<br/>timer while watched" --> ingestion
    alerts -- "via Contracts: reminder" --> ingestion
    trips -- "via Contracts" --> stops
    game -- "via Contracts" --> trips
    stops -- "publish events" --> cluster
    game -- "publish events" --> cluster
    cluster -- "observer per silo,<br/>SSE to the browser" --> browser
    ingestion -- "XML_TRIP_REQUEST2,<br/>departure monitor" --> efa
    ingestion -- "Mode=Replay" --> replay

    subgraph pg [Postgres, one container, schema from db/]
        orleansdb[(orleans<br/>membership + reminders)]
        d1[(stops)]
        d2[(trips)]
        d3[(alerts)]
        d4[(ingestion)]
        d5[(game)]
    end

    replicas --> orleansdb
    stops --> d1
    trips --> d2
    alerts --> d3
    ingestion --> d4
    game --> d5

    host_start{{AppHost}} -- starts --> replicas
    host_start -- starts --> pg
    host_start -- starts --> vite
    replicas -- OTLP --> dashboard[Aspire dashboard]
```

Modules call each other through the `Contracts` projects only. The Cluster module is the hub for live updates: modules publish events to one grain, every silo has one observer on it (subscriptions expire unless renewed), and each replica forwards the events to its browsers as server-sent events, so an event raised on any silo reaches a browser on any replica. Three replicas of the host form one Orleans cluster of identical silos, each with its own silo and gateway port from Aspire. The chaos button kills the replica that receives the request.

The Postgres container loads the Orleans schema from `db/` when it starts on an empty volume. The `orleans` database holds membership and reminders, every module has its own database for its grain state.

Splitting a module into its own deployable later needs only a small host that calls its `Add...Module` and `Map...Endpoints`.
