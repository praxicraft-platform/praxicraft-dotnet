using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Praxicraft.Assess.Resources;

/// <summary>Result endpoints under assessment results and invite result paths.</summary>
public sealed class ResultsResource
{
    private const int MaxResultPages = 10_000;
    private readonly Client _client;

    public ResultsResource(Client client) => _client = client;

    public Task<JsonElement> ListAsync(
        string assessment,
        string? cursor = null,
        int? pageSize = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, object?>();
        if (parameters is not null)
        {
            foreach (var (k, v) in parameters)
            {
                query[k] = v;
            }
        }

        if (cursor is not null) query["cursor"] = cursor;
        if (pageSize is not null) query["page_size"] = pageSize;

        var key = Paths.PathSegment(assessment, "assessment");
        return _client.GetAsync($"/assessments/{key}/results/", query, cancellationToken);
    }

    public Task<JsonElement> RetrieveAsync(string inviteToken, CancellationToken cancellationToken = default)
    {
        var token = Paths.PathSegment(inviteToken, "inviteToken");
        return _client.GetAsync($"/invites/{token}/result/", cancellationToken: cancellationToken);
    }

    public async IAsyncEnumerable<JsonElement> IterAllAsync(
        string assessment,
        int? pageSize = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        string? cursor = null;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < MaxResultPages; i++)
        {
            var page = await ListAsync(assessment, cursor, pageSize, parameters, cancellationToken)
                .ConfigureAwait(false);
            if (page.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }

            if (page.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var row in results.EnumerateArray())
                {
                    yield return row.Clone();
                }
            }

            var next = NextCursorFromPage(page);
            if (next is null || !seen.Add(next))
            {
                yield break;
            }

            cursor = next;
        }
    }

    private static string? NextCursorFromPage(JsonElement page)
    {
        if (page.TryGetProperty("next_cursor", out var nextCursor)
            && nextCursor.ValueKind == JsonValueKind.String)
        {
            var value = nextCursor.GetString();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        if (!page.TryGetProperty("next", out var nextLink) || nextLink.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var link = nextLink.GetString();
        if (string.IsNullOrEmpty(link))
        {
            return null;
        }

        try
        {
            var uri = new Uri(link);
            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && Uri.UnescapeDataString(kv[0]) == "cursor")
                {
                    var c = Uri.UnescapeDataString(kv[1]);
                    return string.IsNullOrEmpty(c) ? null : c;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
