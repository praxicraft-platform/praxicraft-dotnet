using System.Security.Cryptography;
using System.Text;

namespace Praxicraft.Assess;

/// <summary>Helpers for verifying Assess webhook signatures.</summary>
public static class Webhooks
{
    /// <summary>
    /// Verify an <c>X-Praxicraft-Signature</c> header.
    /// Canonical form is <c>sha256=&lt;hex&gt;</c>; legacy raw-hex is also accepted.
    /// A null body is treated as an empty payload.
    /// </summary>
    public static bool VerifySignature(string secret, string? body, string headerSig)
    {
        var bytes = body is null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(body);
        return VerifySignature(secret, bytes, headerSig);
    }

    /// <summary>
    /// Verify an <c>X-Praxicraft-Signature</c> header against a raw body.
    /// A null body is treated as an empty payload.
    /// </summary>
    public static bool VerifySignature(string secret, byte[]? body, string headerSig)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(headerSig))
        {
            return false;
        }

        var payload = body ?? Array.Empty<byte>();
        var digest = ComputeHexDigest(secret, payload);
        var expected = "sha256=" + digest;

        try
        {
            if (headerSig.StartsWith("sha256=", StringComparison.Ordinal))
            {
                return FixedTimeEquals(expected, headerSig);
            }

            return FixedTimeEquals(digest, headerSig) || FixedTimeEquals(expected, headerSig);
        }
        catch
        {
            return false;
        }
    }

    private static string ComputeHexDigest(string secret, byte[] payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        if (aBytes.Length != bBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
