namespace GleisGrain.Stops.Contracts;

/// <summary>
/// The interface version of <see cref="IStopGrain"/> this build was compiled with. Build with
/// <c>-p:GrainVersion=2</c> to get version 2 (compile constant GRAIN_V2), the default is 1.
/// </summary>
public static class StopsGrainVersion
{
#if GRAIN_V2
    public const int Current = 2;
#else
    public const int Current = 1;
#endif
}
