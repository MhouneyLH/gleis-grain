namespace GleisGrain.Cluster.Tests.Support;

internal sealed class FakeProcessKiller : IProcessKiller
{
    public int Kills { get; private set; }

    public void Kill() => Kills++;
}
