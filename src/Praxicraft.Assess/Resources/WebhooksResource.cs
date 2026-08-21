using System.Text.Json;
using Praxicraft.Assess.Errors;

namespace Praxicraft.Assess.Resources;

/// <summary>Webhook endpoint management under <c>/webhooks/</c>.</summary>
public sealed class WebhooksResource
{
    private readonly Client _client;

    public WebhooksResource(Client client) => _client = client;

    public Task<JsonElement> ListAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => _client.GetAsync("/webhooks/", parameters, cancellationToken);

    public Task<JsonElement> CreateAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        if (args is null
            || !args.TryGetValue("url", out var urlObj)
            || string.IsNullOrWhiteSpace(Convert.ToString(urlObj)))
        {
            throw new ApiException("url is required", "INVALID_ARGUMENT");
        }

        if (!args.TryGetValue("events", out var eventsObj) || eventsObj is null)
        {
            throw new ApiException("events must be a non-empty list", "INVALID_ARGUMENT");
        }

        if (eventsObj is System.Collections.IEnumerable enumerable and not string)
        {
            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
                break;
            }

            if (count == 0)
            {
                throw new ApiException("events must be a non-empty list", "INVALID_ARGUMENT");
            }
        }

        return _client.PostAsync("/webhooks/create/", args, cancellationToken);
    }

    public Task<JsonElement> RetrieveAsync(string webhookId, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(webhookId, "webhookId");
        return _client.GetAsync($"/webhooks/{key}/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> UpdateAsync(
        string webhookId,
        IReadOnlyDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
    {
        if (fields is null || fields.Count == 0)
        {
            throw new ApiException("update() requires at least one field to change", "INVALID_ARGUMENT");
        }

        var key = Paths.PathSegment(webhookId, "webhookId");
        return _client.PatchAsync($"/webhooks/{key}/", fields, cancellationToken);
    }

    public Task<JsonElement> DeleteAsync(string webhookId, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(webhookId, "webhookId");
        return _client.DeleteAsync($"/webhooks/{key}/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> DeliveriesAsync(string webhookId, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(webhookId, "webhookId");
        return _client.GetAsync($"/webhooks/{key}/deliveries/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> TestAsync(string webhookId, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(webhookId, "webhookId");
        return _client.PostAsync($"/webhooks/{key}/test/", cancellationToken: cancellationToken);
    }
}
