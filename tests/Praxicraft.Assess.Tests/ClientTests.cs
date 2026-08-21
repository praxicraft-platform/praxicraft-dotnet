using System.Net;
using System.Text;
using Praxicraft.Assess.Errors;
using Xunit;

namespace Praxicraft.Assess.Tests;

public class ClientTests
{
    [Fact]
    public async Task OrgRetrieve_UsesMockHandler()
    {
        var calls = new List<(HttpMethod Method, Uri? Url, string? Auth, string? UserAgent)>();

        using var client = new Client(new ClientOptions
        {
            ApiKey = "ct_test_x",
            HttpHandler = (request, _) =>
            {
                request.Headers.TryGetValues("User-Agent", out var ua);
                calls.Add((
                    request.Method,
                    request.RequestUri,
                    request.Headers.Authorization?.ToString(),
                    ua?.FirstOrDefault()));
                var json = """{"name":"Acme","plan":"starter"}""";
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(response);
            },
        });

        var org = await client.Org.RetrieveAsync();
        Assert.Equal("Acme", org.GetProperty("name").GetString());
        Assert.Equal("starter", org.GetProperty("plan").GetString());
        Assert.Single(calls);
        Assert.Equal(HttpMethod.Get, calls[0].Method);
        Assert.Contains("/api/v1/public/org/", calls[0].Url!.AbsoluteUri);
        Assert.Equal("Bearer ct_test_x", calls[0].Auth);
        Assert.Equal("praxicraft-dotnet/0.1.0", calls[0].UserAgent);
    }

    [Fact]
    public async Task OrgRetrieve_MapsAuthenticationError()
    {
        using var client = new Client(new ClientOptions
        {
            ApiKey = "ct_test_x",
            MaxRetries = 0,
            HttpHandler = (_, _) =>
            {
                var json = """{"error":{"code":"INVALID_API_KEY","message":"bad"}}""";
                var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                return Task.FromResult(response);
            },
        });

        var ex = await Assert.ThrowsAsync<AuthenticationException>(() => client.Org.RetrieveAsync());
        Assert.Equal("INVALID_API_KEY", ex.Code);
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public void MissingApiKey_Throws()
    {
        var previous = Environment.GetEnvironmentVariable("PRAXICRAFT_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("PRAXICRAFT_API_KEY", null);
            var ex = Assert.Throws<ApiException>(() => new Client(new ClientOptions()));
            Assert.Equal("MISSING_API_KEY", ex.Code);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PRAXICRAFT_API_KEY", previous);
        }
    }
}
