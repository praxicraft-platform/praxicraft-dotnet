using System.Text.Json;

namespace Praxicraft.Assess.Resources;

/// <summary>Organisation endpoints under <c>/org/</c>.</summary>
public sealed class OrgResource
{
    private readonly Client _client;

    public OrgResource(Client client) => _client = client;

    /// <summary><c>GET /org/</c> — workspace summary (plan + invite quota).</summary>
    public Task<JsonElement> RetrieveAsync(CancellationToken cancellationToken = default)
        => _client.GetAsync("/org/", cancellationToken: cancellationToken);

    /// <summary><c>GET /org/stats/</c> — aggregate hiring analytics.</summary>
    public Task<JsonElement> StatsAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => _client.GetAsync("/org/stats/", parameters, cancellationToken);
}
