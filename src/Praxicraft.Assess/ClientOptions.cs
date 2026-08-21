namespace Praxicraft.Assess;

/// <summary>Configuration for <see cref="Client"/>.</summary>
public sealed class ClientOptions
{
    /// <summary>Organisation API key. Falls back to <c>PRAXICRAFT_API_KEY</c>.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Assess host. Falls back to <c>PRAXICRAFT_API_BASE_URL</c>, then the production default.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Per-request timeout (default 30s).</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Retries after the first attempt on 429 / 5xx / transport errors (default 2).</summary>
    public int? MaxRetries { get; set; }

    /// <summary>Optional <see cref="HttpMessageHandler"/> for the default <see cref="HttpClient"/>.</summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    /// <summary>
    /// Optional send delegate for tests. When set, it is used instead of <see cref="HttpClient"/>.
    /// Signature: <c>(request, cancellationToken) => response</c>.
    /// </summary>
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? HttpHandler { get; set; }
}
