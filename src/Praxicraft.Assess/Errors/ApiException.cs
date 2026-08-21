namespace Praxicraft.Assess.Errors;

/// <summary>Raised for unexpected API / client failures without an HTTP status.</summary>
public class ApiException : PraxicraftException
{
    public ApiException(string message, string? code = null, Exception? inner = null)
        : base(message, code, inner)
    {
    }
}
