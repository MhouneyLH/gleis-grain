using Microsoft.Extensions.DependencyInjection;

namespace GleisGrain.Postgres.Tests;

/// <summary>
/// Orleans against a real Postgres that was set up with the scripts of the <c>db</c> folder: clustering, grain storage
/// and reminders, each with the real AdoNet provider. All tests skip when Docker is not available.
/// </summary>
[Collection(PostgresTestGroup.Name)]
public class OrleansPostgresTests(PostgresFixture postgres)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Silo_JoinsTheCluster_AsOneActiveMember()
    {
        postgres.RequireDocker();
        const string clusterId = "membership";

        using var host = await SiloFactory.StartAsync(postgres, clusterId);

        // Status 3 is "Active" in the membership protocol.
        var active = await postgres.ScalarAsync(
            "orleans",
            "SELECT count(*) FROM orleansmembershiptable WHERE deploymentid = @id AND status = 3",
            ("id", clusterId));
        Assert.Equal(1, active);
    }

    [Fact]
    public async Task GrainState_SurvivesDeactivation_ThroughAdoNetStorage()
    {
        postgres.RequireDocker();
        using var host = await SiloFactory.StartAsync(postgres, "storage");
        var grain = host.Services.GetRequiredService<IGrainFactory>().GetGrain<ICounterGrain>("hbf");

        await grain.IncrementAsync();
        await grain.IncrementAsync();
        var (_, before) = await grain.ReadAsync();

        await grain.DeactivateAsync();
        var (value, after) = await grain.ReadAsync();

        // A new activation (new id) that got its value from the database.
        Assert.NotEqual(before, after);
        Assert.Equal(2, value);
        Assert.Equal(1, await postgres.ScalarAsync("stops", "SELECT count(*) FROM orleansstorage WHERE serviceid = 'storage'"));
    }

    [Fact]
    public async Task Reminder_Fires_ThroughAdoNetReminderService()
    {
        postgres.RequireDocker();
        using var host = await SiloFactory.StartAsync(postgres, "reminders");
        var grain = host.Services.GetRequiredService<IGrainFactory>().GetGrain<IReminderProbeGrain>("probe");

        await grain.RegisterAsync(TimeSpan.FromSeconds(1));

        using var cancellation = new CancellationTokenSource(Timeout);
        while (await grain.FiredAsync() == 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellation.Token);
        }

        Assert.Equal(1, await postgres.ScalarAsync("orleans", "SELECT count(*) FROM orleansreminderstable WHERE serviceid = 'reminders'"));
    }
}
