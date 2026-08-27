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

    public Task<JsonElement> ListTasksAsync(
        string assessment,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        return _client.GetAsync($"/assessments/{key}/tasks/", parameters, cancellationToken);
    }

    public Task<JsonElement> AttachTasksAsync(
        string assessment,
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        if (args is null || args.Count == 0)
        {
            throw new ApiException("attachTasks() requires tasks or task_id", "INVALID_ARGUMENT");
        }

        var key = Paths.PathSegment(assessment, "assessment");
        return _client.PostAsync($"/assessments/{key}/tasks/attach/", args, cancellationToken);
    }

    public Task<JsonElement> ReplaceTasksAsync(
        string assessment,
        IEnumerable<object> tasks,
        IReadOnlyDictionary<string, object?>? extra = null,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        var body = new Dictionary<string, object?> { ["tasks"] = tasks.ToList() };
        if (extra is not null)
        {
            foreach (var (k, v) in extra)
            {
                body[k] = v;
            }
        }

        return _client.PutAsync($"/assessments/{key}/tasks/replace/", body, cancellationToken);
    }

    public Task<JsonElement> RemoveTaskAsync(
        string assessment,
        string assessmentTaskId,
        CancellationToken cancellationToken = default)
    {
        var key = Paths.PathSegment(assessment, "assessment");
        var taskId = assessmentTaskId?.Trim() ?? "";
        if (taskId.Length == 0)
        {
            throw new ApiException("assessmentTaskId must be a non-empty string", "INVALID_ARGUMENT");
        }

        return _client.DeleteAsync(
            $"/assessments/{key}/tasks/remove/",
            new Dictionary<string, object?> { ["assessment_task_id"] = taskId },
            cancellationToken);
    }
}
