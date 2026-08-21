namespace Praxicraft.Assess.Errors;

/// <summary>Raised when the HTTP request could not be completed.</summary>
public class ApiConnectionException : ApiException
{
    public ApiConnectionException(string message = "Failed to connect to the Praxicraft API.", Exception? inner = null)
        : base(message, "CONNECTION_ERROR", inner)
    {
    }
}
