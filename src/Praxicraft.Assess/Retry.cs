namespace Praxicraft.Assess;

internal static class Retry
{
    public const int DefaultMaxRetries = 2;
    public const int DefaultRetryBaseMs = 500;
    public const int DefaultRetryCapMs = 8_000;

    private static readonly HashSet<int> RetryableStatusCodes = new() { 429, 500, 502, 503, 504 };

    public static bool ShouldRetryStatus(int statusCode) => RetryableStatusCodes.Contains(statusCode);

    /// <summary>Parse Retry-After as delay-seconds or HTTP-date → seconds from now.</summary>
    public static double? ParseRetryAfterSeconds(string? value)
    {
        if (value is null) return null;
        var text = value.Trim();
        if (text.Length == 0) return null;

        if (double.TryParse(text, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var asNumber)
            && !double.IsNaN(asNumber) && !double.IsInfinity(asNumber))
        {
            return Math.Max(0, asNumber);
        }

        if (DateTimeOffset.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var when))
        {
            return Math.Max(0, (when - DateTimeOffset.UtcNow).TotalSeconds);
        }

        return null;
    }

    public static int RetryDelayMs(int attempt, string? retryAfterHeader)
    {
        var parsed = ParseRetryAfterSeconds(retryAfterHeader);
        if (parsed is not null)
        {
            return (int)Math.Min(parsed.Value * 1000, DefaultRetryCapMs);
        }

        var ceiling = Math.Min(DefaultRetryCapMs, DefaultRetryBaseMs * (1 << attempt));
        return Random.Shared.Next(0, Math.Max(1, ceiling + 1));
    }
}
