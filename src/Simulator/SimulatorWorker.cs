using Microsoft.Extensions.Hosting;

namespace GleisGrain.Simulator;

internal sealed class SimulatorWorker(AudienceSimulator simulator, TimeProvider time) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(500);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await simulator.TickAsync(stoppingToken);
            await Task.Delay(TickInterval, time, stoppingToken);
        }
    }
}
