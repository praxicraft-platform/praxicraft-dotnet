using System.Text.Json;

namespace Praxicraft.Assess.Errors;

public class RateLimitException : ApiStatusException
{
    public double? RetryAfter { get; }

    public RateLimitException(
        string message,
        int statusCode,
        string? code = null,
        JsonElement? details = null,
        JsonElement? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string? requiredPlan = null,
        double? retryAfter = null)
        : base(message, statusCode, code, details, responseBody, headers, requiredPlan)
    {
        RetryAfter = retryAfter;
    }
}
