using Aspire.Hosting.Yarp;
using Yarp.ReverseProxy.Configuration;

// One Postgres server, one database per module plus a shared "orleans" database
// for cluster membership and reminders. Image tags are pinned on purpose.
const string PostgresTag = "17.11";
const string PgAdminTag = "9.18";

// The Host runs as several replicas, each one an Orleans silo of the same cluster.
const int HostReplicas = 3;

var builder = DistributedApplication.CreateBuilder(args);

// The host project by path, so the same project can run as two resources. v2 is a second build of it.
var hostProject = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../Host/GleisGrain.Host.csproj"));


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
    // Three replicas keep a pool of up to 20 connections for each of the six databases: 360 in the worst case.
    // The default of 100 makes Postgres refuse connections ("too many clients") during a burst of grain activations.
    .WithArgs("-c", "max_connections=400")
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
//
// Grain versioning demo: the same Host in two builds that join one Orleans cluster.
// gleis-grain-v1 starts with the AppHost. gleis-grain-v2 (compiled with -p:GrainVersion=2, interface version 2
// of the stop grain) waits until the presenter starts it in the Aspire dashboard, then stopping v1 makes the
// stop grains move to the v2 silos.
var hostBuilds = new[]
{
    AddHost("gleis-grain-v1", version: 1, builder.AddProject("gleis-grain-v1", hostProject, ExcludeLaunchProfile)),
    AddHost("gleis-grain-v2", version: 2, builder.AddProject("gleis-grain-v2", hostProject, ExcludeLaunchProfile)
        // The v2 build lives in its own artifacts folder (see GleisGrain.AppHost.csproj), so it never clobbers
        // the v1 output. MSBuild reads both properties from the environment when "dotnet run" evaluates the project.
        .WithEnvironment("GrainVersion", "2")
        .WithEnvironment("ArtifactsPath", Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../artifacts/v2")))
        .WithExplicitStart()),
};

// The launch profile of the Host binds port 5080, which belongs to the gateway here. Endpoints come from the AppHost.
static void ExcludeLaunchProfile(ProjectResourceOptions options) => options.ExcludeLaunchProfile = true;

IResourceBuilder<ProjectResource> AddHost(string name, int version, IResourceBuilder<ProjectResource> project)
{
    project
        .WithReplicas(HostReplicas)
        .WithHttpEndpoint(name: "http")
        .WithEndpoint("silo", endpoint => endpoint.UriScheme = "tcp")
        .WithEndpoint("gateway", endpoint => endpoint.UriScheme = "tcp")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("Orleans__ClusterId", clusterId)
        // Demo settings. The ingestion mode comes from the launch profile (live is the default, replay serves
        // recorded responses): dotnet run --project src/AppHost --launch-profile replay
        .WithEnvironment("Cluster__EnableChaos", "true")
        .WithEnvironment("Ingestion__Mode", builder.Configuration["Ingestion:Mode"] ?? "Live")
        .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/dashboard", DisplayText = "Orleans Dashboard" })
        .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar API docs" })
        .WithReference(orleans)
        .WaitFor(orleans);

    project
        .WithEnvironment("Orleans__SiloPort", project.GetEndpoint("silo").Property(EndpointProperty.TargetPort))
        .WithEnvironment("Orleans__GatewayPort", project.GetEndpoint("gateway").Property(EndpointProperty.TargetPort));

    return project;
}

foreach (var module in new[] { "stops", "trips", "alerts", "ingestion", "game" })
{
    var database = postgres.AddDatabase(module);
    foreach (var hostBuild in hostBuilds)
    {
        hostBuild.WithReference(database).WaitFor(database);
    }
}

// One stable url for the frontend and the browser: a reverse proxy in front of both builds. The Aspire proxy of
// each resource only knows its own replicas, so without this the url would die when v1 is stopped. YARP checks
// the destinations actively and sends requests to the builds that are up. It is named gleis-grain, so the frontend
// keeps reading GLEIS_GRAIN_HTTP.
var gateway = builder.AddYarp("gleis-grain")
    .WithHostPort(int.TryParse(builder.Configuration["Gateway:Port"], out var gatewayPort) ? gatewayPort : 5080)
    .WithConfiguration(yarp =>
    {
        var cluster = yarp.AddCluster("hosts", hostBuilds.Select(hostBuild => (object)hostBuild.GetEndpoint("http")).ToArray())
            .WithHealthCheckConfig(new HealthCheckConfig
            {
                Active = new ActiveHealthCheckConfig
                {
                    Enabled = true,
                    Interval = TimeSpan.FromSeconds(1),
                    Timeout = TimeSpan.FromSeconds(1),
                    Policy = "ConsecutiveFailures",
                    Path = "/api/stops",
                },
                Passive = new PassiveHealthCheckConfig
                {
                    Enabled = true,
                    Policy = "TransportFailureRate",
                },
            });
        yarp.AddRoute("/{**catch-all}", cluster);
    })
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/dashboard", DisplayText = "Orleans Dashboard" })
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar API docs" });

// The proxy container resolves the destinations through service discovery, so it needs the references.
foreach (var hostBuild in hostBuilds)
{
    gateway.WithReference(hostBuild);
}

// Only v1 is awaited: v2 is started by hand, and the gateway must not wait for it.
gateway.WaitFor(hostBuilds[0]);

// The Vue dev server. It reads the Host url from GLEIS_GRAIN_HTTP and proxies /api to it. Aspire writes
// the variable like this for JavaScript apps: npm starts the script through sh, which drops variable
// names with a hyphen such as services__gleis-grain__http__0.
builder.AddViteApp("frontend", "../Frontend")
    .WithReference(gateway)
    .WaitFor(gateway)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Frontend");

await builder.Build().RunAsync();
