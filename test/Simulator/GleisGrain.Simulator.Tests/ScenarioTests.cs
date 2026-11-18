namespace GleisGrain.Simulator.Tests;

public sealed class ScenarioTests
{
    private static readonly string[] Stops = [StopPicker.HauptbahnhofId, "stop-1", "stop-2"];
    private static readonly DateTimeOffset Start = DateTimeOffset.UnixEpoch;

    private static Scenario CreateScenario(int people = 10, int maxPeople = 20)
    {
        var options = new SimulatorOptions { People = people, MaxPeople = maxPeople, RampUpSeconds = 0 };
        return new Scenario(options, Stops, new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1), Start);
    }

    [Fact]
    public void Pin_FewerThanFreePeople_PinsExactlyThatManyWithoutCreatingAnyone()
    {
        var scenario = CreateScenario();

        scenario.Pin("stop-1", 4, Start);

        Assert.Equal(10, scenario.People.Count);
        Assert.Equal(4, scenario.People.Count(person => person.PinnedStopId == "stop-1"));
        Assert.Equal([new PinnedStop("stop-1", 4)], scenario.Pinned);
    }

    [Fact]
    public void Pin_MoreThanFreePeople_CreatesExtraPeopleThatJoinAtOnce()
    {
        var scenario = CreateScenario(people: 3);

        scenario.Pin("stop-1", 8, Start);

        Assert.Equal(8, scenario.People.Count);
        Assert.Equal(8, scenario.People.Select(person => person.Nickname).Distinct().Count());
        Assert.All(scenario.People, person => Assert.Equal(PersonAction.Join, person.Due(Start)));
        Assert.Equal([new PinnedStop("stop-1", 8)], scenario.Pinned);
    }

    [Fact]
    public void Pin_MoreThanMaxPeople_IsRejected()
    {
        var scenario = CreateScenario(maxPeople: 20);

        var ex = Assert.Throws<ScenarioException>(() => scenario.Pin("stop-1", 21, Start));

        Assert.Contains("between 0 and 20", ex.Message, StringComparison.Ordinal);
        Assert.Empty(scenario.Pinned);
    }

    [Fact]
    public void Pin_NegativePeople_IsRejected()
    {
        Assert.Throws<ScenarioException>(() => CreateScenario().Pin("stop-1", -1, Start));
    }

    [Fact]
    public void Pin_UnknownStop_IsRejected()
    {
        var ex = Assert.Throws<ScenarioException>(() => CreateScenario().Pin("nowhere", 1, Start));

        Assert.Contains("nowhere", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Pin_TotalAboveMaxPeople_IsRejectedAndKeepsTheOtherPins()
    {
        var scenario = CreateScenario(maxPeople: 20);
        scenario.Pin("stop-1", 15, Start);

        Assert.Throws<ScenarioException>(() => scenario.Pin("stop-2", 6, Start));

        Assert.Equal([new PinnedStop("stop-1", 15)], scenario.Pinned);
    }

    [Fact]
    public void Pin_LowerThanBefore_ReleasesTheSurplus()
    {
        var scenario = CreateScenario();
        scenario.Pin("stop-1", 6, Start);

        scenario.Pin("stop-1", 2, Start);

        Assert.Equal([new PinnedStop("stop-1", 2)], scenario.Pinned);
        Assert.Equal(10, scenario.People.Count);
    }

    [Fact]
    public void Pin_Zero_RemovesThePin()
    {
        var scenario = CreateScenario();
        scenario.Pin("stop-1", 5, Start);

        scenario.Pin("stop-1", 0, Start);

        Assert.Empty(scenario.Pinned);
    }

    [Fact]
    public void Pin_SamePinTwice_IsIdempotent()
    {
        var scenario = CreateScenario();

        scenario.Pin("stop-1", 5, Start);
        scenario.Pin("stop-1", 5, Start);

        Assert.Equal([new PinnedStop("stop-1", 5)], scenario.Pinned);
        Assert.Equal(10, scenario.People.Count);
    }

    [Fact]
    public void Pin_SeveralStops_ListsThemInStopIdOrder()
    {
        var scenario = CreateScenario();

        scenario.Pin("stop-2", 2, Start);
        scenario.Pin(StopPicker.HauptbahnhofId, 3, Start);

        Assert.Equal([new PinnedStop(StopPicker.HauptbahnhofId, 3), new PinnedStop("stop-2", 2)], scenario.Pinned);
    }

    [Fact]
    public void ReleaseAll_AfterPinning_ReleasesEveryone()
    {
        var scenario = CreateScenario();
        scenario.Pin("stop-1", 3, Start);
        scenario.Pin("stop-2", 3, Start);

        scenario.ReleaseAll(Start);

        Assert.Empty(scenario.Pinned);
        Assert.Equal(10, scenario.People.Count);
    }

    [Fact]
    public void Due_PinnedPerson_GoesToTheStopAndIsNeverMovedRandomly()
    {
        var picker = new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1));
        var person = new VirtualPerson("Sim-001", Start, picker, new Random(1));
        person.Joined("stop-1", Start);

        person.Pin("stop-2", Start);

        Assert.Equal(PersonAction.Move, person.Due(Start));
        Assert.Equal("stop-2", person.ChooseStop());
        person.Moved("stop-2", Start);
        Assert.Equal(PersonAction.None, person.Due(Start + TimeSpan.FromSeconds(5)));
        Assert.Equal(PersonAction.ReadBoard, person.Due(Start + TimeSpan.FromSeconds(18)));
        person.BoardRead(Start + TimeSpan.FromSeconds(18));
        Assert.NotEqual(PersonAction.Move, person.Due(Start + TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void Due_ReleasedPerson_MovesRandomlyAgainAfterTheUsualInterval()
    {
        var picker = new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1));
        var person = new VirtualPerson("Sim-001", Start, picker, new Random(1));
        person.Joined("stop-1", Start);
        person.Pin("stop-1", Start);

        person.Release(Start);

        Assert.Null(person.PinnedStopId);
        Assert.NotEqual(PersonAction.Move, person.Due(Start + TimeSpan.FromSeconds(19)));
        Assert.Equal(PersonAction.Move, person.Due(Start + TimeSpan.FromSeconds(61)));
    }

    [Fact]
    public void Pin_PersonStillWaitingToJoin_JoinsAtOnce()
    {
        var picker = new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1));
        var person = new VirtualPerson("Sim-001", Start + TimeSpan.FromSeconds(30), picker, new Random(1));

        person.Pin("stop-1", Start);

        Assert.Equal(PersonAction.Join, person.Due(Start));
        Assert.Equal("stop-1", person.ChooseStop());
    }
}
