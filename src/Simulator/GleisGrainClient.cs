using System.Net;
using System.Net.Http.Json;

namespace GleisGrain.Simulator;

/// <summary>The public HTTP API as a phone page uses it. Failures surface as <see cref="HttpRequestException"/>.</summary>
public sealed class GleisGrainClient(HttpClient http)
{
    public async Task<IReadOnlyList<string>> GetStopIdsAsync(CancellationToken cancellationToken)
    {
        var stops = await http.GetFromJsonAsync<List<StopEntry>>(new Uri("api/stops", UriKind.Relative), cancellationToken);
        return stops?.Select(stop => stop.Id).ToList() ?? [];
    }

    /// <summary>Registers the nickname. A nickname that exists already is fine: a retry, or an earlier run.</summary>
    public async Task ClaimNicknameAsync(string nickname, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(new Uri("api/game/players", UriKind.Relative), new { nickname }, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Conflict)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task MoveToStopAsync(string nickname, string stopId, CancellationToken cancellationToken)
    {
        var uri = new Uri($"api/game/players/{Uri.EscapeDataString(nickname)}/stop", UriKind.Relative);
        using var response = await http.PutAsJsonAsync(uri, new { stopId }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Reads the board of a stop, which activates the stop grain.</summary>
    public async Task ReadBoardAsync(string stopId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri($"api/stops/{Uri.EscapeDataString(stopId)}", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record StopEntry(string Id);
}
