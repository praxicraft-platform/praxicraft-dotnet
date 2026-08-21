using System.Text.Json;
using Praxicraft.Assess.Errors;

namespace Praxicraft.Assess.Resources;

/// <summary>Hiring pipeline endpoints under <c>/pipelines/</c>.</summary>
public sealed class PipelinesResource
{
    private readonly Client _client;

    public PipelinesResource(Client client) => _client = client;

    public Task<JsonElement> ListAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => _client.GetAsync("/pipelines/", parameters, cancellationToken);

    public Task<JsonElement> RetrieveAsync(string pipeline, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(pipeline, "pipeline");
        return _client.GetAsync($"/pipelines/{key}/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> EnrollAsync(
        string pipeline,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        if (args is null
            || !args.TryGetValue("email", out var emailObj)
            || string.IsNullOrWhiteSpace(Convert.ToString(emailObj)))
        {
            throw new ApiException("email is required", "INVALID_ARGUMENT");
        }

        var key = Paths.PathSegment(pipeline, "pipeline");
        return _client.PostAsync($"/pipelines/{key}/enroll/", args, cancellationToken);
    }

    public Task<JsonElement> BulkEnrollAsync(
        string pipeline,
        IEnumerable<object> candidates,
        IReadOnlyDictionary<string, object?>? args = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(pipeline, "pipeline");
        var body = new Dictionary<string, object?> { ["candidates"] = candidates.ToList() };
        if (args is not null)
        {
            foreach (var (k, v) in args)
            {
                body[k] = v;
            }
        }

        return _client.PostAsync($"/pipelines/{key}/enroll/bulk/", body, cancellationToken);
    }

    public Task<JsonElement> ListEnrollmentsAsync(
        string pipeline,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(pipeline, "pipeline");
        return _client.GetAsync($"/pipelines/{key}/enrollments/", parameters, cancellationToken);
    }

    public Task<JsonElement> GetEnrollmentAsync(string enrollmentId, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(enrollmentId, "enrollmentId");
        return _client.GetAsync($"/pipelines/enrollments/{key}/", cancellationToken: cancellationToken);
    }
}
