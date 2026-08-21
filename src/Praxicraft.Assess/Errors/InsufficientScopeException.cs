using System.Text.Json;

namespace Praxicraft.Assess.Errors;

public class InsufficientScopeException : ApiStatusException
{
    public InsufficientScopeException(
        string message,
        int statusCode,
        string? code = null,
        JsonElement? details = null,
        JsonElement? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string? requiredPlan = null)
        : base(message, statusCode, code, details, responseBody, headers, requiredPlan)
    {
    }
}
