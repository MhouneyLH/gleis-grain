using GleisGrain.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Dashboard;

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
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<OrleansOptions>(OrleansOptions.SectionName);

        var options = builder.Configuration.GetSection(OrleansOptions.SectionName).Get<OrleansOptions>() ?? new OrleansOptions();
        var clusteringConnection = builder.Configuration.GetConnectionString(options.ClusteringConnectionName)
            ?? throw new InvalidOperationException($"Connection string '{options.ClusteringConnectionName}' is missing.");
        var storages = storageNames.ToDictionary(
            name => name,
            name => builder.Configuration.GetConnectionString(name)
                ?? throw new InvalidOperationException($"Connection string '{name}' is missing."));

        builder.UseOrleans(silo =>
        {
            silo.Configure<ClusterOptions>(cluster =>
            {
                cluster.ClusterId = options.ClusterId;
                cluster.ServiceId = options.ServiceId;
            });

            silo.ConfigureEndpoints(siloPort: options.SiloPort, gatewayPort: options.GatewayPort);

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
}
