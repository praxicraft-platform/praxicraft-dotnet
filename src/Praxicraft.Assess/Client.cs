using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Praxicraft.Assess.Errors;
using Praxicraft.Assess.Resources;

namespace Praxicraft.Assess;

/// <summary>HTTP client for the Praxicraft Assess Public API.</summary>
public sealed class Client : IDisposable
{
    public const string DefaultBaseUrl = "https://assess.praxicraft.com";
    public const string DefaultApiPrefix = "/api/v1/public";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient? _httpClient;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _httpHandler;
    private readonly bool _disposeHttpClient;

    public string ApiKey { get; }
    public string BaseUrl { get; }
    public string ApiPrefix { get; } = DefaultApiPrefix;
    public TimeSpan Timeout { get; }
    public int MaxRetries { get; }

    public OrgResource Org { get; }
    public AssessmentsResource Assessments { get; }
    public InvitesResource Invites { get; }
    public ResultsResource Results { get; }
    public WebhooksResource Webhooks { get; }
    public PipelinesResource Pipelines { get; }

    public Client()
        : this(new ClientOptions())
    {
    }

    public Client(ClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var resolvedKey = (options.ApiKey ?? Environment.GetEnvironmentVariable("PRAXICRAFT_API_KEY") ?? "").Trim();
        if (resolvedKey.Length == 0)
        {
            throw new ApiException(
                "No API key provided. Pass apiKey or set PRAXICRAFT_API_KEY.",
                "MISSING_API_KEY");
        }

        var resolvedBase = (options.BaseUrl
            ?? Environment.GetEnvironmentVariable("PRAXICRAFT_API_BASE_URL")
            ?? DefaultBaseUrl).Trim().TrimEnd('/');
        if (resolvedBase.Length == 0)
        {
            throw new ApiException("baseUrl must be a non-empty URL.", "INVALID_BASE_URL");
        }

        ApiKey = resolvedKey;
        BaseUrl = resolvedBase;
        Timeout = options.Timeout ?? DefaultTimeout;
        MaxRetries = Math.Max(0, options.MaxRetries ?? Retry.DefaultMaxRetries);
        _httpHandler = options.HttpHandler;

        if (_httpHandler is null)
        {
            _httpClient = options.HttpMessageHandler is null
                ? new HttpClient()
                : new HttpClient(options.HttpMessageHandler, disposeHandler: false);
            _httpClient.Timeout = Timeout;
            _disposeHttpClient = true;
        }

        Org = new OrgResource(this);
        Assessments = new AssessmentsResource(this);
        Invites = new InvitesResource(this);
        Results = new ResultsResource(this);
        Webhooks = new WebhooksResource(this);
        Pipelines = new PipelinesResource(this);
    }

    public Task<JsonElement> GetAsync(
        string path,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => RequestAsync(HttpMethod.Get, path, parameters, json: null, cancellationToken);

    public Task<JsonElement> PostAsync(
        string path,
        object? json = null,
        CancellationToken cancellationToken = default)
        => RequestAsync(HttpMethod.Post, path, parameters: null, json, cancellationToken);

    public Task<JsonElement> PutAsync(
        string path,
        object? json = null,
        CancellationToken cancellationToken = default)
        => RequestAsync(HttpMethod.Put, path, parameters: null, json, cancellationToken);

    public Task<JsonElement> PatchAsync(
        string path,
        object? json = null,
        CancellationToken cancellationToken = default)
        => RequestAsync(HttpMethod.Patch, path, parameters: null, json, cancellationToken);

    public Task<JsonElement> DeleteAsync(
        string path,
        object? json = null,
        CancellationToken cancellationToken = default)
        => RequestAsync(HttpMethod.Delete, path, parameters: null, json, cancellationToken);

    public async Task<JsonElement> RequestAsync(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, object?>? parameters = null,
        object? json = null,
        CancellationToken cancellationToken = default)
    {
        var attempts = MaxRetries + 1;
        Exception? lastError = null;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (attempt > 0)
            {
                string? retryAfter = null;
                if (lastError is ApiStatusException status)
                {
                    status.Headers.TryGetValue("retry-after", out retryAfter);
                }

                var delayMs = Retry.RetryDelayMs(attempt - 1, retryAfter);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                return await RequestOnceAsync(method, path, parameters, json, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ApiConnectionException ex)
            {
                lastError = ex;
                if (attempt < attempts - 1)
                {
                    continue;
                }

                throw;
            }
            catch (ApiStatusException ex)
            {
                lastError = ex;
                if (Retry.ShouldRetryStatus(ex.StatusCode) && attempt < attempts - 1)
                {
                    continue;
                }

                throw;
            }
        }

        throw lastError ?? new ApiConnectionException();
    }

    private async Task<JsonElement> RequestOnceAsync(
        HttpMethod method,
        string path,
        IReadOnlyDictionary<string, object?>? parameters,
        object? json,
        CancellationToken cancellationToken)
    {
        var url = BuildUrl(path, parameters);
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        request.Headers.TryAddWithoutValidation("User-Agent", $"praxicraft-dotnet/{Version.String}");

        if (json is not null)
        {
            var body = JsonSerializer.Serialize(json, JsonOptions);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_httpHandler is null)
            {
                cts.CancelAfter(Timeout);
            }

            if (_httpHandler is not null)
            {
                response = await _httpHandler(request, cts.Token).ConfigureAwait(false);
            }
            else
            {
                response = await _httpClient!.SendAsync(request, cts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiConnectionException($"Request timed out: {ex.Message}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiConnectionException($"Transport error: {ex.Message}", ex);
        }
        catch (ApiConnectionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApiConnectionException($"Transport error: {ex.Message}", ex);
        }

        using (response)
        {
            var headerMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers)
            {
                headerMap[header.Key] = string.Join(",", header.Value);
            }

            foreach (var header in response.Content.Headers)
            {
                headerMap[header.Key] = string.Join(",", header.Value);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                // Match Node: empty success body is JSON null.
                return JsonDocument.Parse("null").RootElement.Clone();
            }

            var rawText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            JsonElement? parsed = null;
            if (!string.IsNullOrEmpty(rawText))
            {
                try
                {
                    using var doc = JsonDocument.Parse(rawText);
                    parsed = doc.RootElement.Clone();
                }
                catch (JsonException)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        throw new ApiException(
                            $"Invalid JSON response (HTTP {(int)response.StatusCode}).",
                            "INVALID_JSON");
                    }
                }
            }

            if (response.IsSuccessStatusCode)
            {
                return parsed ?? default;
            }

            throw ErrorMapper.RaiseForStatus(
                (int)response.StatusCode,
                parsed,
                headerMap,
                rawText);
        }
    }

    private string BuildUrl(string path, IReadOnlyDictionary<string, object?>? parameters)
    {
        string absolute;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            absolute = path;
        }
        else
        {
            var normalized = path.StartsWith('/') ? path : "/" + path;
            if (!normalized.StartsWith(ApiPrefix, StringComparison.Ordinal))
            {
                normalized = ApiPrefix + normalized;
            }

            absolute = BaseUrl + normalized;
        }

        if (parameters is null || parameters.Count == 0)
        {
            return absolute;
        }

        var builder = new UriBuilder(absolute);
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(builder.Query))
        {
            var existing = builder.Query.TrimStart('?');
            if (existing.Length > 0)
            {
                parts.Add(existing);
            }
        }

        foreach (var (key, value) in parameters)
        {
            if (value is null) continue;
            var encodedKey = Uri.EscapeDataString(key);
            string encodedValue;
            if (value is bool b)
            {
                encodedValue = b ? "true" : "false";
            }
            else
            {
                encodedValue = Uri.EscapeDataString(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "");
            }

            parts.Add(encodedKey + "=" + encodedValue);
        }

        builder.Query = string.Join("&", parts);
        return builder.Uri.ToString();
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}
