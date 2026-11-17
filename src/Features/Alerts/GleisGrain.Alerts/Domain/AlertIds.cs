using System.Security.Cryptography;
using System.Text;
using GleisGrain.Alerts.Contracts;

namespace GleisGrain.Alerts.Domain;

internal static class AlertIds
{
    /// <summary>
    /// Notice ids come from the source and may not fit the rules of an <see cref="AlertId"/>.
    /// A valid id is kept, any other one becomes "h-" and a hash of it, so the same notice always gets the same id.
    /// </summary>
    public static AlertId From(string noticeId) =>
        AlertId.TryParse(noticeId, null, out var id)
            ? id
            : AlertId.From("h-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(noticeId)))[..32]);
}
