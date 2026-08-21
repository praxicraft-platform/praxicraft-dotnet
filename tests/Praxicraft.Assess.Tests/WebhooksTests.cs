using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Praxicraft.Assess.Tests;

public class WebhooksTests
{
    private static string HexDigest(string secret, string body)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var payload = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, payload);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void VerifySignature_Prefixed()
    {
        const string secret = "whsec_test";
        const string body = """{"ok":true}""";
        var digest = HexDigest(secret, body);
        Assert.True(Webhooks.VerifySignature(secret, body, "sha256=" + digest));
    }

    [Fact]
    public void VerifySignature_LegacyHex()
    {
        const string secret = "whsec_test";
        const string body = """{"ok":true}""";
        var digest = HexDigest(secret, body);
        Assert.True(Webhooks.VerifySignature(secret, body, digest));
    }

    [Fact]
    public void VerifySignature_RejectsBad()
    {
        Assert.False(Webhooks.VerifySignature("whsec_test", "{}", "sha256=deadbeef"));
    }

    [Fact]
    public void VerifySignature_NullBodyIsEmpty()
    {
        const string secret = "whsec_test";
        var digest = HexDigest(secret, "");
        Assert.True(Webhooks.VerifySignature(secret, (string?)null, "sha256=" + digest));
        Assert.True(Webhooks.VerifySignature(secret, (byte[]?)null, "sha256=" + digest));
    }

    [Fact]
    public void VerifySignature_ByteArrayBody()
    {
        const string secret = "whsec_test";
        const string bodyText = """{"ok":true}""";
        var body = Encoding.UTF8.GetBytes(bodyText);
        var digest = HexDigest(secret, bodyText);
        Assert.True(Webhooks.VerifySignature(secret, body, "sha256=" + digest));
    }
}
