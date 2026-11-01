// One Postgres server, one database per module plus a shared "orleans" database
// for cluster membership and reminders. Image tags are pinned on purpose.
const string PostgresTag = "17.11";
const string PgAdminTag = "9.18";

var builder = DistributedApplication.CreateBuilder(args);

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
    .WithPgAdmin(pgAdmin => pgAdmin.WithImageTag(PgAdminTag));

var orleans = postgres.AddDatabase("orleans");

// Modular monolith: one host that contains all modules. The database keeps the plain module name
// because it becomes the connection string name the module reads.
var host = builder.AddProject<Projects.GleisGrain_Host>("gleis-grain")
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/dashboard", DisplayText = "Orleans Dashboard" })
    .WithUrlForEndpoint("http", endpoint => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar API docs" })
    .WithReference(orleans)
    .WaitFor(orleans);

foreach (var module in new[] { "stops", "trips", "alerts", "ingestion" })
{
    var database = postgres.AddDatabase(module);
    host.WithReference(database).WaitFor(database);
}

await builder.Build().RunAsync();
