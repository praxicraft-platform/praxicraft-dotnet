namespace Praxicraft.Assess.Errors;

/// <summary>Base exception for all SDK errors.</summary>
public class PraxicraftException : Exception
{
    public string? Code { get; }

    public PraxicraftException(string message, string? code = null, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }
}
