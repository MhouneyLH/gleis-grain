// One Postgres server, one database per module plus a shared "orleans" database
// for cluster membership and reminders. Image tags are pinned on purpose.
const string PostgresTag = "17.11";
const string PgAdminTag = "9.18";

// The Host runs as several replicas, each one an Orleans silo of the same cluster.
const int HostReplicas = 3;

var builder = DistributedApplication.CreateBuilder(args);

// Every AppHost start is a new Orleans cluster. Stopping the AppHost kills the silos before they can mark
// themselves dead, and a new silo refuses to join while the membership table lists unreachable active silos
// of its cluster id. The service id stays, so grain state and reminders survive the restart.
var clusterId = $"gleis-grain-{DateTime.UtcNow:yyyyMMddHHmmss}";

// The data volume keeps the password of its first start, so a new random password on every run
// would lock everyone out. persist: true keeps the generated value in the AppHost user secrets.
var postgresPassword = builder.AddParameter(
    "postgres-password",
    new GenerateParameterDefault { MinLength = 24, Special = false },
    secret: true,
    persist: true);

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImageTag(PostgresTag)
    .WithDataVolume("gleis-grain-postgres")
    // db/init.sh creates the databases and loads the Orleans schema. Postgres runs it only on an empty
    // data volume, so after a schema change: docker volume rm gleis-grain-postgres
    .WithInitFiles("../../db")
    .WithPgAdmin(pgAdmin => pgAdmin.WithImageTag(PgAdminTag));

var orleans = postgres.AddDatabase("orleans");

// Modular monolith: one host that contains all modules. The database keeps the plain module name
// because it becomes the connection string name the module reads.
// Every replica is a silo and needs its own silo and gateway port. These endpoints are not proxied,
// so Aspire picks a free port per replica and the Host binds exactly that one. The "http" endpoint
// stays proxied: Aspire load balances it over the replicas.
var host = builder.AddProject<Projects.GleisGrain_Host>("gleis-grain")
    .WithReplicas(HostReplicas)
    .WithEndpoint("silo", endpoint => endpoint.UriScheme = "tcp")
    .WithEndpoint("gateway", endpoint => endpoint.UriScheme = "tcp")
    .WithEnvironment("Orleans__ClusterId", clusterId)
    // Demo settings. The ingestion mode can be changed per run, for example
    // dotnet run --project src/AppHost -- Ingestion:Mode=Replay
    .WithEnvironment("Cluster__EnableChaos", "true")
    .WithEnvironment("Ingestion__Mode", builder.Configuration["Ingestion:Mode"] ?? "Live")
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/dashboard", DisplayText = "Orleans Dashboard" })
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar API docs" })
    .WithReference(orleans)
    .WaitFor(orleans);

host.WithEnvironment("Orleans__SiloPort", host.GetEndpoint("silo").Property(EndpointProperty.TargetPort))
    .WithEnvironment("Orleans__GatewayPort", host.GetEndpoint("gateway").Property(EndpointProperty.TargetPort));

foreach (var module in new[] { "stops", "trips", "alerts", "ingestion", "game" })
{
    var database = postgres.AddDatabase(module);
    host.WithReference(database).WaitFor(database);
}

// The Vue dev server. It reads the Host url from GLEIS_GRAIN_HTTP and proxies /api to it. Aspire writes
// the variable like this for JavaScript apps: npm starts the script through sh, which drops variable
// names with a hyphen such as services__gleis-grain__http__0.
builder.AddViteApp("frontend", "../Frontend")
    .WithReference(host)
    .WaitFor(host)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Frontend");

await builder.Build().RunAsync();
