using System.Diagnostics;

namespace GleisGrain.Cluster;

internal sealed class ProcessKiller : IProcessKiller
{
    /// <summary>Time to send the 202 answer before the process disappears.</summary>
    private static readonly TimeSpan _delay = TimeSpan.FromMilliseconds(500);

    // SIGKILL on purpose: Environment.Exit runs the ProcessExit handlers, and the host would then shut the silo down
    // gracefully. The demo needs the cluster to find out about the death through membership, like after a real crash.
    public void Kill() =>
        _ = Task.Run(async () =>
        {
            await Task.Delay(_delay);
            Process.GetCurrentProcess().Kill();
        });
}
