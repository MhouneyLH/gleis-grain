using GleisGrain.Ingestion.Contracts;

namespace GleisGrain.Alerts.Tests;

/// <summary>
/// Stands in for the Ingestion module: the grain implementation of <see cref="ISourceGrain"/> in this assembly.
/// What it answers is static because Orleans creates the grain itself; tests that use it must not run in parallel
/// (they share the collection <see cref="Name"/>).
/// </summary>
internal sealed class FakeSourceGrain : Grain, ISourceGrain
{
    public const string Name = "Fake source grain";

    public static List<NoticeSnapshot> Notices { get; set; } = [];

    public static Exception? Failure { get; set; }

    public static int NoticeCalls { get; set; }

    public static void Reset()
    {
        Notices = [];
        Failure = null;
        NoticeCalls = 0;
    }

    public static NoticeSnapshot Notice(string id, string title, string[]? lines = null, string[]? stopIds = null) =>
        new(id, title, "Text of " + title, lines ?? [], stopIds ?? []);

    public Task<IReadOnlyList<NoticeSnapshot>> GetNoticesAsync()
    {
        NoticeCalls++;

        return Failure is null
            ? Task.FromResult<IReadOnlyList<NoticeSnapshot>>(Notices.ToList())
            : Task.FromException<IReadOnlyList<NoticeSnapshot>>(Failure);
    }

    public Task<IReadOnlyList<DepartureSnapshot>> GetDeparturesAsync(string efaStopId) =>
        throw new NotSupportedException("Alerts never asks for departures.");

    public Task<SourceStats> GetStatsAsync() => Task.FromResult(new SourceStats(NoticeCalls, 0, null));
}
