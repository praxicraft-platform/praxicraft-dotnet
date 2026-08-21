using System.Text.Json;

namespace Praxicraft.Assess.Errors;

/// <summary>Raised for non-success HTTP responses from the Assess Public API.</summary>
public class ApiStatusException : ApiException
{
    public int StatusCode { get; }
    public JsonElement? Details { get; }
    public JsonElement? ResponseBody { get; }
    public IReadOnlyDictionary<string, string> Headers { get; }
    public string? RequiredPlan { get; }

    public ApiStatusException(
        string message,
        int statusCode,
        string? code = null,
        JsonElement? details = null,
        JsonElement? responseBody = null,
        IReadOnlyDictionary<string, string>? headers = null,
        string? requiredPlan = null)
        : base(message, code)
    {
        StatusCode = statusCode;
        Details = details;
        ResponseBody = responseBody;
        Headers = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        RequiredPlan = requiredPlan;
    }
}
