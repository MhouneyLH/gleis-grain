using Npgsql;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;

namespace GleisGrain.Postgres.Tests;

/// <summary>
/// One Postgres container for all tests of the collection. It starts like the one of the AppHost:
/// the <c>db</c> folder is copied to <c>/docker-entrypoint-initdb.d</c>, so <c>db/init.sh</c> creates the databases
/// and loads the Orleans schema. The tests skip when Docker is not available.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // Same image as the AppHost.
    private const string Image = "postgres:17.11";

    private PostgreSqlContainer? container;
    private string? dockerUnavailable;

    /// <summary>Skips the calling test with a clear message when there is no Docker.</summary>
    public void RequireDocker() =>
        Assert.SkipWhen(dockerUnavailable is not null, $"Docker is not available, so the Postgres tests are skipped: {dockerUnavailable}");

    /// <summary>The connection string of one database of the container, for example <c>orleans</c> or <c>stops</c>.</summary>
    public string ConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(container!.GetConnectionString()) { Database = database, Pooling = false }.ConnectionString;

    /// <summary>Runs a query that returns one number.</summary>
    public async Task<long> ScalarAsync(string database, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(ConnectionString(database));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async ValueTask InitializeAsync()
    {
        // Building the container already needs Docker, so this is inside the try block.
        try
        {
            container = new PostgreSqlBuilder(Image)
                .WithResourceMapping(DirectoryPath.Of(Path.Combine(AppContext.BaseDirectory, "db")), DirectoryPath.Of("/docker-entrypoint-initdb.d"))
                .Build();
            await container.StartAsync();
        }
        catch (DockerUnavailableException exception)
        {
            dockerUnavailable = exception.Message;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresTestGroup : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
