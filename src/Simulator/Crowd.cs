namespace GleisGrain.Simulator;

public static class Crowd
{
    /// <summary>When the person with the given index (from 0) joins: evenly spread over the ramp-up, the first one right away.</summary>
    public static TimeSpan JoinOffset(int index, int people, TimeSpan rampUp) => rampUp * index / people;

    public static IReadOnlyList<VirtualPerson> Create(SimulatorOptions options, StopPicker picker, Random random, DateTimeOffset start)
    {
        ArgumentNullException.ThrowIfNull(options);
        var rampUp = TimeSpan.FromSeconds(options.RampUpSeconds);
        return
        [
            .. Enumerable.Range(0, options.People).Select(index => new VirtualPerson(
                Nicknames.For(index + 1),
                start + JoinOffset(index, options.People, rampUp),
                picker,
                random)),
        ];
    }
}
