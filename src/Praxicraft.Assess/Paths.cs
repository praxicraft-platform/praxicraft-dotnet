using Praxicraft.Assess.Errors;

namespace Praxicraft.Assess;

internal static class Paths
{
    public static string PathSegment(string? value, string label = "id")
    {
        if (value is null)
        {
            throw new ApiException($"{label} is required", "INVALID_PATH");
        }

        var text = value.Trim();
        if (text.Length == 0)
        {
            throw new ApiException($"{label} must be a non-empty string", "INVALID_PATH");
        }

        return Uri.EscapeDataString(text);
    }
}
