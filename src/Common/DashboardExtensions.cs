using GleisGrain.Common.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Dashboard;

namespace GleisGrain.Common;

public static class DashboardExtensions
{
    /// <summary>
    /// Maps the Orleans Dashboard when <c>Orleans:EnableDashboard</c> is true.
    /// The silo side (<c>AddDashboard</c>) is registered by <see cref="OrleansExtensions"/>.
    /// </summary>
    public static WebApplication MapGleisGrainDashboard(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<OrleansOptions>>().Value;
        if (options.EnableDashboard)
        {
            app.MapOrleansDashboard(options.DashboardPath);
        }

        return app;
    }
}
