using GleisGrain.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Dashboard;
using Orleans.Versions.Compatibility;
using Orleans.Versions.Selector;

namespace GleisGrain.Common;

public static class OrleansExtensions
{
    private const string PostgresInvariant = "Npgsql";

    /// <summary>
    /// Configures one silo of the shared gleis-grain cluster.
    /// Membership and reminders live in the shared <c>orleans</c> database, grain state in the feature's own database.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <param name="storageNames">Grain storage names, one per module; by convention equal to the module's connection string name.</param>
    public static TBuilder AddGleisGrainOrleans<TBuilder>(this TBuilder builder, params string[] storageNames)
        where TBuilder : IHostApplicationBuilder =>
        builder.AddGleisGrainOrleans(1, storageNames);

    /// <summary>
    /// Configures one silo of the shared gleis-grain cluster that carries the given grain interface version
    /// of its build. The version becomes the prefix of the silo name, see <see cref="SiloNames"/>.
    /// </summary>
    /// <param name="builder">The host builder to configure.</param>
    /// <param name="grainVersion">The grain interface version this build was compiled with, 1 or 2.</param>
    /// <param name="storageNames">Grain storage names, one per module; by convention equal to the module's connection string name.</param>
    public static TBuilder AddGleisGrainOrleans<TBuilder>(this TBuilder builder, int grainVersion, params string[] storageNames)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<OrleansOptions>(OrleansOptions.SectionName);

        var options = builder.Configuration.GetSection(OrleansOptions.SectionName).Get<OrleansOptions>() ?? new OrleansOptions();
        var clusteringConnection = WithPoolLimit(
            builder.Configuration.GetConnectionString(options.ClusteringConnectionName)
                ?? throw new InvalidOperationException($"Connection string '{options.ClusteringConnectionName}' is missing."),
            options.MaxConnectionPoolSize);
        var storages = storageNames.ToDictionary(
            name => name,
            name => WithPoolLimit(
                builder.Configuration.GetConnectionString(name)
                    ?? throw new InvalidOperationException($"Connection string '{name}' is missing."),
                options.MaxConnectionPoolSize));

        builder.UseOrleans(silo =>
        {
            silo.Configure<ClusterOptions>(cluster =>
            {
                cluster.ClusterId = options.ClusterId;
                cluster.ServiceId = options.ServiceId;
            });

            // v<version>-<random>: the frontend reads the grain version of a silo from the name prefix.
            silo.Configure<SiloOptions>(o => o.SiloName = SiloNames.Create(grainVersion));

            // Rolling upgrade of grain interfaces: a v1 caller may use a v2 grain (BackwardCompatible), and a
            // new activation goes to a silo with the newest interface version (LatestVersion).
            silo.Configure<GrainVersioningOptions>(o =>
            {
                o.DefaultCompatibilityStrategy = nameof(BackwardCompatible);
                o.DefaultVersionSelectorStrategy = nameof(LatestVersion);
            });

            silo.ConfigureEndpoints(siloPort: options.SiloPort, gatewayPort: options.GatewayPort);

            // Carries the trace context through grain calls, so one request shows up as one trace across grains and silos.
            silo.AddActivityPropagation();

            if (options.EnableDashboard)
            {
                silo.AddDashboard();
            }

            silo.UseAdoNetClustering(o =>
            {
                o.Invariant = PostgresInvariant;
                o.ConnectionString = clusteringConnection;
            });

            silo.UseAdoNetReminderService(o =>
            {
                o.Invariant = PostgresInvariant;
                o.ConnectionString = clusteringConnection;
            });

            foreach (var (name, connectionString) in storages)
            {
                silo.AddAdoNetGrainStorage(name, o =>
                {
                    o.Invariant = PostgresInvariant;
                    o.ConnectionString = connectionString;
                });
            }
        });

        return builder;
    }

    private static string WithPoolLimit(string connectionString, int maxPoolSize) =>
        new Npgsql.NpgsqlConnectionStringBuilder(connectionString) { MaxPoolSize = maxPoolSize }.ConnectionString;
}
