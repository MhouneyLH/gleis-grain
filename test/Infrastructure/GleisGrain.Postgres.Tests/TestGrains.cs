
namespace GleisGrain.Postgres.Tests;

/// <summary>A grain with persistent state in the "stops" storage, to prove that AdoNet storage works.</summary>
public interface ICounterGrain : IGrainWithStringKey
{
    Task<int> IncrementAsync();

    /// <summary>The counter and the id of the activation that answered.</summary>
    Task<(int Value, Guid Activation)> ReadAsync();

    Task DeactivateAsync();
}

/// <summary>A grain that registers a reminder, to prove that the AdoNet reminder service fires it.</summary>
public interface IReminderProbeGrain : IGrainWithStringKey
{
    Task RegisterAsync(TimeSpan period);

    Task<int> FiredAsync();
}

[GenerateSerializer]
public sealed class CounterState
{
    [Id(0)]
    public int Value { get; set; }
}

internal sealed class CounterGrain(
    [PersistentState("counter", SiloFactory.StorageName)] IPersistentState<CounterState> state) : Grain, ICounterGrain
{
    private readonly Guid activation = Guid.NewGuid();

    public async Task<int> IncrementAsync()
    {
        state.State.Value++;
        await state.WriteStateAsync();
        return state.State.Value;
    }

    public Task<(int Value, Guid Activation)> ReadAsync() => Task.FromResult((state.State.Value, activation));

    public Task DeactivateAsync()
    {
        DeactivateOnIdle();
        return Task.CompletedTask;
    }
}

internal sealed class ReminderProbeGrain : Grain, IReminderProbeGrain, IRemindable
{
    private int fired;

    public async Task RegisterAsync(TimeSpan period) =>
        await this.RegisterOrUpdateReminder("probe", dueTime: period, period: period);

    public Task<int> FiredAsync() => Task.FromResult(fired);

    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        fired++;
        return Task.CompletedTask;
    }
}
