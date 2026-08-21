using System.Text.Json;

namespace Praxicraft.Assess.Errors;

internal static class ErrorMapper
{
    public static Exception RaiseForStatus(
        int statusCode,
        JsonElement? body,
        IReadOnlyDictionary<string, string> headers,
        string rawText)
    {
        string? code = null;
        string? message = null;
        JsonElement? details = null;
        string? requiredPlan = null;

        if (body is { ValueKind: JsonValueKind.Object } obj
            && obj.TryGetProperty("error", out var errorEl)
            && errorEl.ValueKind == JsonValueKind.Object)
        {
            if (errorEl.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
            {
                code = codeEl.GetString();
            }

            if (errorEl.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
            {
                message = msgEl.GetString();
            }

            if (errorEl.TryGetProperty("details", out var detailsEl))
            {
                details = detailsEl.Clone();
            }

            if (errorEl.TryGetProperty("required_plan", out var planEl) && planEl.ValueKind == JsonValueKind.String)
            {
                requiredPlan = planEl.GetString();
            }
        }

        if (string.IsNullOrEmpty(message))
        {
            var trimmed = rawText.Trim();
            message = trimmed.Length > 0
                ? trimmed[..Math.Min(500, trimmed.Length)]
                : $"API request failed with status {statusCode}.";
        }

        headers.TryGetValue("retry-after", out var retryAfterHeader);

        if (statusCode == 401)
        {
            return new AuthenticationException(message, statusCode, code, details, body, headers, requiredPlan);
        }

        if (statusCode == 403)
        {
            return new InsufficientScopeException(message, statusCode, code, details, body, headers, requiredPlan);
        }

        if (statusCode == 404)
        {
            return new NotFoundException(message, statusCode, code, details, body, headers, requiredPlan);
        }

        if (statusCode == 429)
        {
            return new RateLimitException(
                message,
                statusCode,
                code,
                details,
                body,
                headers,
                requiredPlan,
                Retry.ParseRetryAfterSeconds(retryAfterHeader));
        }

        if (statusCode is >= 400 and < 500)
        {
            return new ValidationException(message, statusCode, code, details, body, headers, requiredPlan);
        }

        return new ApiStatusException(message, statusCode, code, details, body, headers, requiredPlan);
    }
}
