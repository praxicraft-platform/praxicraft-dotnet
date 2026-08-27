# Praxicraft Assess .NET SDK

Official .NET client for the **[Praxicraft Assess](https://assess.praxicraft.com)** Public API.

Use it to invite candidates, check invite quota, manage webhooks, enroll hiring pipelines, and fetch results from your ATS, backend, or automation scripts.

```bash
dotnet add package Praxicraft.Assess
```

Until NuGet publish, pack from source:

```bash
git clone https://github.com/praxicraft-platform/praxicraft-dotnet.git
cd praxicraft-dotnet && dotnet pack -c Release
```

**Requires .NET 8+.** Full API reference: [docs.praxicraft.com/sdks/dotnet](https://docs.praxicraft.com/sdks/dotnet)

## Table of Contents

- [Authentication](#authentication)
- [Quickstart](#quickstart)
- [What you can do](#what-you-can-do)
  - [Check invite quota before bulk sends](#check-invite-quota-before-bulk-sends)
  - [Bulk invites](#bulk-invites)
  - [Build and activate an assessment via API](#build-and-activate-an-assessment-via-api)
  - [Register and test a webhook](#register-and-test-a-webhook)
  - [Enroll into a hiring pipeline](#enroll-into-a-hiring-pipeline)
  - [Paginate cohort results](#paginate-cohort-results)
  - [Verify webhook signatures](#verify-webhook-signatures)
- [Errors](#errors)
- [Requirements & support](#requirements--support)
- [License](#license)

---

## Authentication

Create an organisation API key in Assess:

**Assess → Developer → API Keys** → create key → copy `ct_live_…` (shown once).

```bash
export PRAXICRAFT_API_KEY="ct_live_xxxxxxxxxxxxxxxx"
```

Or pass the key when constructing the client:

```csharp
using Praxicraft.Assess;

using var client = new Client(new ClientOptions
{
    ApiKey = "ct_live_xxxxxxxxxxxxxxxx",
});
```

Optional: override the API host with `PRAXICRAFT_API_BASE_URL` or `ClientOptions.BaseUrl`.
Default host: `https://assess.praxicraft.com`.

Never commit API keys. Prefer environment variables or a secrets manager.

Scopes and rotation: [Authentication](https://docs.praxicraft.com/authentication)

---

## Quickstart

```csharp
using System.Text.Json;
using Praxicraft.Assess;

using var client = new Client(); // reads PRAXICRAFT_API_KEY

JsonElement page = await client.Assessments.ListAsync();
foreach (var assessment in page.GetProperty("results").EnumerateArray())
{
    Console.WriteLine($"{assessment.GetProperty("slug")} {assessment.GetProperty("status")}");
}

// Invite a candidate (idempotent on email — safe to retry)
JsonElement invite = await client.Invites.CreateAsync(
    "senior-backend-screen",
    new Dictionary<string, object?>
    {
        ["email"] = "candidate@example.com",
        ["name"] = "Jane Doe",
        ["send_email"] = true,
    });

string token = invite.GetProperty("invite_token").GetString()!;
Console.WriteLine(token);

JsonElement result = await client.Results.RetrieveAsync(token);
Console.WriteLine(result);
```

Responses are **flat JSON** (same shape as the Public API — no `{ "data": … }` wrapper).

---

## What you can do

| Resource | Common methods |
|----------|----------------|
| `client.Org` | `RetrieveAsync()`, `StatsAsync()` |
| `client.Assessments` | `ListAsync()`, `RetrieveAsync()`, `CreateAsync()`, `UpdateAsync()`, `ActivateAsync()`, `ListTasksAsync()`, `AttachTasksAsync()`, `ReplaceTasksAsync()`, `RemoveTaskAsync()` |
| `client.Invites` | `CreateAsync()`, `BulkCreateAsync()`, `ListAsync()`, `RetrieveAsync()`, `RemindAsync()`, `CancelAsync()` |
| `client.Results` | `ListAsync()`, `RetrieveAsync()`, `IterAllAsync()` |
| `client.Webhooks` | `ListAsync()`, `CreateAsync()`, `RetrieveAsync()`, `UpdateAsync()`, `DeleteAsync()`, `TestAsync()`, `DeliveriesAsync()` |
| `client.Pipelines` | `ListAsync()`, `RetrieveAsync()`, `EnrollAsync()`, `BulkEnrollAsync()`, `ListEnrollmentsAsync()`, `GetEnrollmentAsync()` |
| `Webhooks.VerifySignature` | Verify `X-Praxicraft-Signature` on webhook payloads |

All paths target `/api/v1/public/…` on the Assess host.

### Check invite quota before bulk sends

```csharp
JsonElement org = await client.Org.RetrieveAsync();
int remaining = org.TryGetProperty("invites_remaining", out var ir) ? ir.GetInt32() : 0;
if (remaining < candidates.Count)
{
    throw new InvalidOperationException("Not enough invites remaining this month");
}
```

### Bulk invites

```csharp
await client.Invites.BulkCreateAsync(
    "senior-backend-screen",
    new object[]
    {
        new Dictionary<string, object?> { ["email"] = "a@example.com", ["name"] = "Alex" },
        new Dictionary<string, object?> { ["email"] = "b@example.com", ["name"] = "Blair" },
    },
    new Dictionary<string, object?> { ["send_email"] = true });
```

### Build and activate an assessment via API

```csharp
JsonElement assessment = await client.Assessments.CreateAsync(
    new Dictionary<string, object?> { ["title"] = "Backend screen" });
string slug = assessment.GetProperty("slug").GetString()!;

await client.Assessments.AttachTasksAsync(
    slug,
    new Dictionary<string, object?>
    {
        ["tasks"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["task_id"] = "<platform-or-org-task-uuid>",
                ["source"] = "platform",
            },
        },
    });
await client.Assessments.ActivateAsync(slug);
```

### Register and test a webhook

```csharp
JsonElement hook = await client.Webhooks.CreateAsync(new Dictionary<string, object?>
{
    ["url"] = "https://example.com/hooks/praxicraft",
    ["events"] = new[] { "assessment.completed", "candidate.passed" },
});
// Store hook.GetProperty("secret_key") (whsec_…) — shown once
string id = hook.GetProperty("id").GetString()!;
await client.Webhooks.TestAsync(id);
await client.Webhooks.UpdateAsync(id, new Dictionary<string, object?> { ["is_active"] = true });
```

### Enroll into a hiring pipeline

```csharp
JsonElement enrollment = await client.Pipelines.EnrollAsync(
    "grad-2025",
    new Dictionary<string, object?>
    {
        ["email"] = "alex@example.com",
        ["name"] = "Alex Lee",
        ["send_email"] = true,
    });
JsonElement status = await client.Pipelines.GetEnrollmentAsync(
    enrollment.GetProperty("enrollment_id").GetString()!);
```

### Paginate cohort results

```csharp
await foreach (JsonElement row in client.Results.IterAllAsync(
    "senior-backend-screen",
    new Dictionary<string, object?> { ["page_size"] = 50 }))
{
    Console.WriteLine(row);
}
```

### Verify webhook signatures

Assess signs the **raw request body** with your webhook secret (`whsec_…`):

```csharp
using Praxicraft.Assess;

bool ok = Webhooks.VerifySignature(secret, rawBody, signatureHeader);
```

Header format: `X-Praxicraft-Signature: sha256=<hex>`

Event catalog and payload examples: [Webhooks](https://docs.praxicraft.com/webhooks)

---

## Errors

Public API errors look like:

```json
{
  "error": {
    "code": "INSUFFICIENT_SCOPE",
    "message": "This API key does not have the 'candidates:read' scope."
  }
}
```

The SDK raises typed exceptions. **Branch on `ex.Code`**, not the message text:

```csharp
using Praxicraft.Assess.Errors;

try
{
    await client.Invites.CreateAsync("demo", new Dictionary<string, object?>
    {
        ["email"] = "candidate@example.com",
    });
}
catch (ValidationException ex)
{
    Console.WriteLine(ex.Code);
    Console.WriteLine(ex.Details);
}
catch (InsufficientScopeException ex)
{
    Console.WriteLine(ex.Code);
}
catch (AuthenticationException ex)
{
    Console.WriteLine(ex.Code);
}
catch (RateLimitException ex)
{
    Console.WriteLine(ex.RetryAfter);
}
```

Error codes: [Errors](https://docs.praxicraft.com/errors)

---

## Requirements & support

- .NET **8.0+**
- Uses `HttpClient` + `System.Text.Json`
- Product docs: [docs.praxicraft.com](https://docs.praxicraft.com)
- Issues: [GitHub Issues](https://github.com/praxicraft-platform/praxicraft-dotnet/issues)

---

## License

[MIT](LICENSE)
