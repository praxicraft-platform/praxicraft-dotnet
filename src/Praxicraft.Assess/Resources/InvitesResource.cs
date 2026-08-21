using System.Text.Json;
using Praxicraft.Assess.Errors;

namespace Praxicraft.Assess.Resources;

/// <summary>Invite endpoints under <c>/invites/</c> and <c>/assessments/{slug}/invites/</c>.</summary>
public sealed class InvitesResource
{
    private readonly Client _client;

    public InvitesResource(Client client) => _client = client;

    public Task<JsonElement> ListAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => _client.GetAsync("/invites/", parameters, cancellationToken);

    public Task<JsonElement> RetrieveAsync(string inviteToken, CancellationToken cancellationToken = default)
    {
        var token = Paths.PathSegment(inviteToken, "inviteToken");
        return _client.GetAsync($"/invites/{token}/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> CreateAsync(
        string assessment,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        if (args is null
            || !args.TryGetValue("email", out var emailObj)
            || string.IsNullOrWhiteSpace(Convert.ToString(emailObj)))
        {
            throw new ApiException("email is required", "INVALID_ARGUMENT");
        }

        var key = Paths.PathSegment(assessment, "assessment");
        return _client.PostAsync($"/assessments/{key}/invites/", args, cancellationToken);
    }

    public Task<JsonElement> BulkCreateAsync(
        string assessment,
        IEnumerable<object> candidates,
        IReadOnlyDictionary<string, object?>? args = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        var body = new Dictionary<string, object?> { ["candidates"] = candidates.ToList() };
        if (args is not null)
        {
            foreach (var (k, v) in args)
            {
                body[k] = v;
            }
        }

        return _client.PostAsync($"/assessments/{key}/invites/bulk/", body, cancellationToken);
    }

    public Task<JsonElement> RemindAsync(string inviteToken, CancellationToken cancellationToken = default)
    {
        var token = Paths.PathSegment(inviteToken, "inviteToken");
        return _client.PostAsync($"/invites/{token}/remind/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> CancelAsync(string inviteToken, CancellationToken cancellationToken = default)
    {
        var token = Paths.PathSegment(inviteToken, "inviteToken");
        return _client.DeleteAsync($"/invites/{token}/", cancellationToken: cancellationToken);
    }
}
