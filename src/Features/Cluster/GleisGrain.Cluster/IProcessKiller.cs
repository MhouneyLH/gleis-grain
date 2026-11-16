namespace GleisGrain.Cluster;

/// <summary>Ends the process without a graceful shutdown. Replaceable so tests do not kill the test host.</summary>
internal interface IProcessKiller
{
    /// <summary>Starts the kill and returns at once, so the caller can still answer its request.</summary>
    void Kill();
}
