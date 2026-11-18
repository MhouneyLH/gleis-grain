using System.Text.RegularExpressions;

namespace GleisGrain.Simulator.Tests;

public sealed partial class CrowdTests
{
    private static readonly string[] Stops = [StopPicker.HauptbahnhofId, .. Enumerable.Range(1, 24).Select(n => $"stop-{n}")];

    [GeneratedRegex("^[A-Za-z0-9 _-]{2,20}$")]
    private static partial Regex ValidNickname();

    [Fact]
    public void Pick_FixedSeed_HonoursTheHotShareAndReachesEveryStop()
    {
        var picker = new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1));

        var picks = Enumerable.Range(0, 10_000).Select(_ => picker.Pick()).ToList();

        var hotShare = picks.Count(id => id == StopPicker.HauptbahnhofId) / 10_000.0;
        Assert.InRange(hotShare, 0.37, 0.43);
        Assert.Equal(Stops.Length, picks.Distinct().Count());
    }

    [Fact]
    public void Pick_HotShareZero_NeverPicksTheHotStop()
    {
        var picker = new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.0, new Random(1));

        Assert.DoesNotContain(StopPicker.HauptbahnhofId, Enumerable.Range(0, 1_000).Select(_ => picker.Pick()));
    }

    [Fact]
    public void Pick_HotStopMissingFromCatalogue_FallsBackToTheCatalogue()
    {
        var picker = new StopPicker(["a", "b"], StopPicker.HauptbahnhofId, 1.0, new Random(1));

        Assert.All(Enumerable.Range(0, 100).Select(_ => picker.Pick()), id => Assert.Contains(id, new[] { "a", "b" }));
    }

    [Fact]
    public void PickOther_HotShareOne_NeverReturnsTheCurrentStop()
    {
        var picker = new StopPicker(Stops, StopPicker.HauptbahnhofId, 1.0, new Random(1));

        Assert.All(Enumerable.Range(0, 100).Select(_ => picker.PickOther(StopPicker.HauptbahnhofId)), id => Assert.NotEqual(StopPicker.HauptbahnhofId, id));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(80)]
    [InlineData(9999)]
    public void For_Number_ReturnsAValidNickname(int number)
    {
        Assert.Matches(ValidNickname(), Nicknames.For(number));
    }

    [Fact]
    public void Create_DefaultOptions_CreatesEightyUniqueNicknames()
    {
        var crowd = Crowd.Create(new SimulatorOptions(), new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1), DateTimeOffset.UnixEpoch);

        Assert.Equal(80, crowd.Count);
        Assert.Equal(80, crowd.Select(person => person.Nickname).Distinct().Count());
        Assert.Equal("Sim-001", crowd[0].Nickname);
        Assert.Equal("Sim-080", crowd[79].Nickname);
    }

    [Fact]
    public void Due_RampUp_PeopleJoinGradually()
    {
        var start = DateTimeOffset.UnixEpoch;
        var options = new SimulatorOptions { People = 80, RampUpSeconds = 40 };
        var crowd = Crowd.Create(options, new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1), start);

        Assert.Equal(1, crowd.Count(person => person.Due(start) == PersonAction.Join));
        Assert.Equal(41, crowd.Count(person => person.Due(start + TimeSpan.FromSeconds(20)) == PersonAction.Join));
        Assert.Equal(80, crowd.Count(person => person.Due(start + TimeSpan.FromSeconds(40)) == PersonAction.Join));
    }

    [Fact]
    public void Due_AfterJoining_MovesWithin20To60SecondsAndReadsEvery12To18()
    {
        var start = DateTimeOffset.UnixEpoch;
        var person = new VirtualPerson("Sim-001", start, new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1));
        person.Joined("stop-1", start);

        Assert.Equal(PersonAction.None, person.Due(start + TimeSpan.FromSeconds(11)));
        Assert.Equal(PersonAction.ReadBoard, person.Due(start + TimeSpan.FromSeconds(18)));
        Assert.Equal(PersonAction.Move, person.Due(start + TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public void Due_AfterReadingTheBoard_WaitsForTheNextInterval()
    {
        var start = DateTimeOffset.UnixEpoch;
        var person = new VirtualPerson("Sim-001", start, new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1));
        person.Joined("stop-1", start);
        var readAt = start + TimeSpan.FromSeconds(12);

        person.BoardRead(readAt);

        Assert.Equal(PersonAction.None, person.Due(readAt + TimeSpan.FromSeconds(11)));
        Assert.NotEqual(PersonAction.None, person.Due(readAt + TimeSpan.FromSeconds(18)));
    }

    [Fact]
    public void ChooseStop_AfterJoining_PicksAnotherStop()
    {
        var start = DateTimeOffset.UnixEpoch;
        var person = new VirtualPerson("Sim-001", start, new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1));
        person.Joined("stop-1", start);

        Assert.All(Enumerable.Range(0, 50).Select(_ => person.ChooseStop()), id => Assert.NotEqual("stop-1", id));
    }

    [Fact]
    public void Due_AfterFailure_WaitsBeforeRetrying()
    {
        var start = DateTimeOffset.UnixEpoch;
        var person = new VirtualPerson("Sim-001", start, new StopPicker(Stops, StopPicker.HauptbahnhofId, 0.4, new Random(1)), new Random(1));

        person.Failed(start);

        Assert.Equal(PersonAction.None, person.Due(start + TimeSpan.FromSeconds(1)));
        Assert.Equal(PersonAction.Join, person.Due(start + TimeSpan.FromSeconds(5)));
    }
}
