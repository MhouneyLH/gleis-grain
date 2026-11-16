using GleisGrain.Common.Options;
using GleisGrain.Game.Endpoints;

namespace GleisGrain.Game;

/// <summary>
/// Public surface of the Game module. Everything else in this project is internal.
/// The host calls <see cref="AddGameModule{TBuilder}"/> and <see cref="MapGameEndpoints"/>.
/// </summary>
public static class GameModule
{
    /// <summary>Grain storage name, equals the connection string name of the game database.</summary>
    public const string StorageName = GameOptions.StorageName;

    /// <summary>Name of the meter the module publishes metrics on.</summary>
    public const string MeterName = GameMetrics.MeterName;

    public static TBuilder AddGameModule<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddValidatedOptions<GameOptions>(GameOptions.SectionName);
        builder.Services.AddSingleton<GameMetrics>();

        return builder;
    }

    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/game");
        ClaimPlayer.Map(api);
        GetPlayer.Map(api);
        WatchStop.Map(api);
        PlaceBet.Map(api);
        GetLeaderboard.Map(api);

        return routes;
    }
}
