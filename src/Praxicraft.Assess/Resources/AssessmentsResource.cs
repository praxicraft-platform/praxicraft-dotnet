using System.Text.Json;
using Praxicraft.Assess.Errors;

namespace Praxicraft.Assess.Resources;

/// <summary>Assessment endpoints under <c>/assessments/</c>.</summary>
public sealed class AssessmentsResource
{
    private readonly Client _client;

    public AssessmentsResource(Client client) => _client = client;

    public Task<JsonElement> ListAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
        => _client.GetAsync("/assessments/", parameters, cancellationToken);

    public Task<JsonElement> RetrieveAsync(string assessment, CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        return _client.GetAsync($"/assessments/{key}/", cancellationToken: cancellationToken);
    }

    public Task<JsonElement> CreateAsync(
        IReadOnlyDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
        => _client.PostAsync("/assessments/create/", fields, cancellationToken);

    public Task<JsonElement> UpdateAsync(
        string assessment,
        IReadOnlyDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
    {
        if (fields is null || fields.Count == 0)
        {
            throw new ApiException("update() requires at least one field to change", "INVALID_ARGUMENT");
        }

        var key = Paths.PathSegment(assessment, "assessment");
        return _client.PatchAsync($"/assessments/{key}/update/", fields, cancellationToken);
    }

    public Task<JsonElement> ActivateAsync(string assessment, CancellationToken cancellationToken = default)
        => UpdateAsync(assessment, new Dictionary<string, object?> { ["status"] = "active" }, cancellationToken);

    public Task<JsonElement> ListCasesAsync(
        string assessment,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        return _client.GetAsync($"/assessments/{key}/cases/", parameters, cancellationToken);
    }

    public Task<JsonElement> AttachCasesAsync(
        string assessment,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        if (args is null || args.Count == 0)
        {
            throw new ApiException("attachCases() requires cases or case_id", "INVALID_ARGUMENT");
        }

        var key = Paths.PathSegment(assessment, "assessment");
        return _client.PostAsync($"/assessments/{key}/cases/attach/", args, cancellationToken);
    }

    public Task<JsonElement> ReplaceCasesAsync(
        string assessment,
        IEnumerable<object> cases,
        IReadOnlyDictionary<string, object?>? extra = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        var body = new Dictionary<string, object?> { ["cases"] = cases.ToList() };
        if (extra is not null)
        {
            foreach (var (k, v) in extra)
            {
                body[k] = v;
            }
        }

        return _client.PutAsync($"/assessments/{key}/cases/replace/", body, cancellationToken);
    }

    public Task<JsonElement> RemoveCaseAsync(
        string assessment,
        string assessmentCaseId,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        var caseId = assessmentCaseId?.Trim() ?? "";
        if (caseId.Length == 0)
        {
            throw new ApiException("assessmentCaseId must be a non-empty string", "INVALID_ARGUMENT");
        }

        return _client.DeleteAsync(
            $"/assessments/{key}/cases/remove/",
            new Dictionary<string, object?> { ["assessment_case_id"] = caseId },
            cancellationToken);
    }
}
