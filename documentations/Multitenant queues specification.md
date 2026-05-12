# Multitenant Queues Specification

## Summary

Hangfire queues are currently global within a storage instance. If multiple tenants enqueue jobs to queue `a`, all jobs land in the same logical queue and any server configured for queue `a` can fetch them. This specification describes an opt-in multitenant queue model where queue identity includes a tenant scope.

With multitenant queues enabled, two tenants can both use queue name `a` while keeping their queued jobs distinct:

```text
Tenant XYZ, Queue a
Tenant ABC, Queue a
```

A server can be configured to process queue `a` only for tenant `XYZ`, while another server processes queue `a` only for tenant `ABC`. Existing Hangfire behavior must remain unchanged unless multitenancy is explicitly enabled.

## Goals

- Add an opt-in tenant dimension to queue identity.
- Allow the same queue name to exist independently for different tenants.
- Allow servers to process queues for a specific tenant.
- Preserve current non-tenant behavior by default.
- Replace the server queue declaration with a priority-aware model, even if this requires a major-version breaking change.
- Make tenant scope visible in monitoring and dashboard views.
- Define how tenant-aware queues interact with resource awareness, drain mode, and queue availability.
- Make queue priority deterministic and explicit.
- Avoid cross-tenant job fetching when tenant-aware queues are enabled.

## Non-Goals

- Do not enable multitenancy by default.
- Do not break existing queue names, jobs, workers, or storage records.
- Do not require every Hangfire deployment to define tenants.
- Do not implement tenant billing or identity management.
- Do not provide full tenant data isolation across every Hangfire entity in the first phase.
- Do not guarantee full tenant data isolation across every Hangfire entity in the first phase.
- Do not replace Hangfire's pull-based worker model.
- Do not implement central scheduling or global resource accounting.

## Current Behavior

Today, a queue is effectively identified by its name:

```text
Queue = "a"
```

All servers using the same storage and listening to queue `a` share that queue.

Example:

```text
Tenant XYZ enqueues queue a -> JobQueue.Queue = "a"
Tenant ABC enqueues queue a -> JobQueue.Queue = "a"
Server listening to queue a -> can fetch both jobs
```

Applications can simulate tenant separation by encoding the tenant into the queue name:

```text
xyz_a
abc_a
```

This works operationally but is not first-class multitenancy. It leaks tenant identity into queue names, complicates dashboard views, makes routing conventions application-specific, and does not provide a shared model for monitoring or resource policies.

## Desired Behavior

When multitenant queues are enabled, queue identity becomes:

```text
TenantId + Queue
```

Example:

```text
TenantId = "xyz", Queue = "a"
TenantId = "abc", Queue = "a"
```

A server configured for tenant `xyz` and queue `a` fetches only jobs where:

```text
TenantId = "xyz"
Queue = "a"
```

A server configured for tenant `abc` and queue `a` fetches only jobs where:

```text
TenantId = "abc"
Queue = "a"
```

Existing non-tenant servers continue to use the global queue namespace:

```text
TenantId = null
Queue = "a"
```

## Compatibility Rule

The central compatibility rule is:

```text
TenantId == null means legacy/global behavior.
```

If no tenant is configured:

- Existing enqueue behavior remains unchanged.
- Existing worker fetch behavior remains unchanged.
- Existing dashboard queue views remain valid.
- Existing jobs in queues are still processed normally.
- Existing storage records remain readable.

Tenant-aware behavior is activated only when a tenant id is explicitly configured for job creation, server processing, or both.

## Terminology

- **Tenant**: A logical application-defined partition, identified by a string tenant id.
- **Global queue**: A legacy queue with no tenant id.
- **Tenant queue**: A queue scoped to a tenant id.
- **Logical queue name**: The user-facing queue name, such as `a` or `emails`.
- **Physical queue identity**: The storage-level identity used to fetch jobs, including tenant id when enabled.
- **Tenant-aware server**: A server configured to fetch jobs for a specific tenant scope.
- **Tenant-aware job**: A job enqueued with tenant metadata.

## Public API Direction

### Server Options

Add an optional tenant id to `BackgroundJobServerOptions` and replace the string-array queue declaration with a priority-aware collection:

```csharp
public sealed class BackgroundJobServerOptions
{
    public string TenantId { get; set; }
    public QueuePriorityCollection Queues { get; set; }
}

public sealed class QueuePriorityCollection : IDictionary<string, int>
{
    // Key: logical queue name.
    // Value: priority, where a lower number is higher priority.
}
```

Behavior:

- `TenantId == null` preserves current behavior.
- `TenantId != null` means the server fetches only tenant-scoped jobs for that single tenant.
- Each server instance is scoped to either global queues or one tenant; processing multiple tenants on the same server is out of scope for the first implementation.
- `Queues` maps logical queue names to explicit integer priorities.
- Priority value `1` is the highest priority. Larger numbers are lower priority.
- Every declared queue must have an explicit priority.
- The queue API change is accepted as a major-version breaking change.

Example:

```csharp
services.AddHangfireServer(options =>
{
    options.TenantId = "xyz";
    options.Queues =
    [
        ["a"] = 1,
        ["emails"] = 2
    ];
});
```

Another node:

```csharp
services.AddHangfireServer(options =>
{
    options.TenantId = "abc";
    options.Queues =
    [
        ["a"] = 1,
        ["emails"] = 2
    ];
});
```

### Client Options

Job creation needs a way to associate a tenant with enqueued jobs.

Possible option:

```csharp
public sealed class BackgroundJobClientOptions
{
    public string TenantId { get; set; }
}
```

Possible explicit API:

```csharp
BackgroundJob
    .ForTenant("xyz")
    .Enqueue(() => SendEmail());
```

Possible scoped API:

```csharp
using (HangfireTenantContext.Use("xyz"))
{
    BackgroundJob.Enqueue(() => SendEmail());
}
```

Recommended direction:

1. Provide an explicit job creation option for infrastructure code.
2. Provide a scoped context helper for applications that already resolve tenant context per request.
3. Persist tenant id in job parameters/state data so it is visible and recoverable.

### Tenant Context

Candidate API:

```csharp
public static class HangfireTenantContext
{
    public static IDisposable Use(string tenantId);
    public static string CurrentTenantId { get; }
}
```

Behavior:

- The current tenant should flow through async calls.
- Nested scopes should restore the previous tenant on dispose.
- Empty or whitespace tenant ids should be rejected.
- The context should not affect existing code unless used.

### ASP.NET Core Tenant Provider

Core Hangfire should remain based on explicit tenant APIs and `HangfireTenantContext`. ASP.NET Core integration should add a small tenant provider abstraction for applications that already resolve tenant identity per request.

Candidate API in `Hangfire.AspNetCore`:

```csharp
public interface ITenantIdProvider
{
    string GetTenantId();
}
```

Optional async-friendly shape if request resolution requires I/O:

```csharp
public interface IAsyncTenantIdProvider
{
    ValueTask<string> GetTenantIdAsync(CancellationToken cancellationToken);
}
```

Recommended behavior:

- `ITenantIdProvider` is registered through ASP.NET Core dependency injection.
- The provider is used by ASP.NET Core convenience APIs and filters only.
- Core Hangfire does not depend on ASP.NET Core abstractions.
- Explicit tenant APIs override provider-derived tenant ids.
- Missing provider or null result means no tenant id unless an application-specific validation filter rejects it.
- Provider output must pass the same tenant id validation rules as explicit APIs.

### Queue Attribute

Existing queue attributes specify queue name only:

```csharp
[Queue("a")]
public void SendEmail() { }
```

Tenant should not be added directly to `QueueAttribute` in the first phase, because tenant is usually runtime data and attributes are static.

Recommended behavior:

- `QueueAttribute` continues to select the logical queue name.
- Tenant id comes from client options, tenant context, or explicit enqueue API.

## Tenant Validation

Tenant ids should have validation rules separate from queue names.

Suggested allowed characters:

```text
lowercase letters, digits, underscore, dash, dot
```

Suggested regex:

```text
^[a-z0-9_.-]+$
```

Rationale:

- Tenant ids may map to slugs or external account ids.
- They should be storage-safe.
- They should avoid whitespace and case ambiguity.

Tenant id length should be limited. Suggested maximum:

```text
100 characters
```

Queue names keep existing validation rules.

## Storage Model

### Preferred Model: Tenant Column

The cleanest SQL Server model adds a nullable tenant column to queue storage.

Current conceptual `JobQueue`:

```text
Id
JobId
Queue
FetchedAt
```

Tenant-aware conceptual `JobQueue`:

```text
Id
JobId
TenantId nullable
Queue
FetchedAt
```

Legacy jobs:

```text
TenantId = null
Queue = "a"
```

Tenant jobs:

```text
TenantId = "xyz"
Queue = "a"
```

Fetch condition for global server:

```sql
where TenantId is null
and Queue in @queues
```

Fetch condition for tenant-aware server:

```sql
where TenantId = @tenantId
and Queue in @queues
```

### Alternative Model: Encoded Physical Queue Name

An alternative prototype can avoid schema migration by encoding tenant id into the physical queue string:

```text
tenant:xyz:a
tenant:abc:a
```

The dashboard can decode this and display:

```text
Tenant xyz, Queue a
Tenant abc, Queue a
```

Advantages:

- No SQL schema change.
- Reuses existing queue storage and indexing.
- Easier first implementation.

Disadvantages:

- Physical queue names become implementation details.
- Existing queue validation may reject encoded names unless encoding uses allowed characters.
- Monitoring APIs must decode names.
- External storage providers may have their own queue name constraints.
- It is easier for users to accidentally depend on encoded names.

### Decision

Use the tenant-column model for the production implementation. Tenant id is a first-class storage dimension, not part of the queue name.

Encoded physical queue names are allowed only for experimental prototypes. They must not become the public storage contract, must not be required for storage providers, and must not appear in dashboard or monitoring APIs as the canonical queue identity.

## SQL Server Schema Direction

SQL Server storage requires a migration for production multitenant queue support.

Add nullable column:

```sql
alter table [HangFire].JobQueue
add TenantId nvarchar(100) null;
```

Recommended index direction:

```sql
create index IX_HangFire_JobQueue_Tenant_QueueAndFetchedAt
on [HangFire].JobQueue (TenantId, Queue, FetchedAt)
include (JobId);
```

The exact index must be evaluated against current Hangfire SQL schema, dequeue query shape, and supported SQL Server versions.

Compatibility:

- Existing rows have `TenantId = null`.
- Existing fetch queries remain valid for global servers.
- New tenant-aware fetch queries use tenant-specific predicates.
- Schema migration is not required for legacy/global-only mode.
- Schema migration is required before enabling production multitenant queue support for SQL Server.

## Storage Abstraction Changes

Current fetch shape:

```csharp
IFetchedJob FetchNextJob(string[] queues, CancellationToken cancellationToken);
```

Priority-aware, tenant-aware fetch can be introduced with queue descriptors:

```csharp
public sealed class QueueDescriptor
{
    public string Name { get; }
    public int Priority { get; }
}
```

Fetch overload:

```csharp
public virtual IFetchedJob FetchNextJob(
    string tenantId,
    QueueDescriptor[] queues,
    CancellationToken cancellationToken)
{
    if (tenantId != null)
    {
        throw new NotSupportedException("Current storage doesn't support tenant-aware queues.");
    }

    return FetchNextJob(queues.Select(queue => queue.Name).ToArray(), cancellationToken);
}
```

Worker behavior:

- If `TenantId == null`, fetch only global jobs using the priority-aware queue declaration.
- If `TenantId != null`, fetch only jobs for that one tenant using the same priority-aware queue declaration.
- Providers that do not implement the new fetch shape must fail clearly when priority-aware or tenant-aware mode is enabled.

This is a breaking storage contract for providers that participate in the new model. Providers can keep legacy behavior for existing deployments that do not opt into the new major-version APIs.

### Enqueue Abstraction

Current enqueue shape:

```csharp
void AddToQueue(string queue, string jobId);
```

Tenant-aware enqueue can use a virtual overload:

```csharp
public virtual void AddToQueue(string tenantId, string queue, string jobId)
{
    if (tenantId != null)
    {
        throw new NotSupportedException("Current storage doesn't support tenant-aware queues.");
    }

    AddToQueue(queue, jobId);
}
```

Alternatively, tenant id can be passed through state data and interpreted by the storage-specific `EnqueuedState` handler.

Recommended direction:

- Add tenant-aware overloads at the storage abstraction layer.
- Preserve existing methods as the legacy/global path.

## Job Metadata

Tenant-aware jobs should persist tenant id in job metadata.

Candidate job parameter:

```text
TenantId = xyz
```

Candidate state data:

```text
Queue = a
TenantId = xyz
EnqueuedAt = ...
```

Rationale:

- Dashboard can show tenant even outside queue views.
- Retry and state transitions can preserve tenant identity.
- Debugging is easier.
- Future features can filter by tenant.

Tenant id should be immutable for a job once enqueued unless a future explicit migration feature is designed.

## Worker Behavior

### Global Server

Configuration:

```csharp
options.TenantId = null;
options.Queues =
[
    ["a"] = 1
];
```

Fetches:

```text
TenantId = null
Queue = a
```

Does not fetch:

```text
TenantId = xyz
Queue = a
```

This avoids a global legacy server accidentally stealing tenant-scoped jobs.

### Tenant-Aware Server

Configuration:

```csharp
options.TenantId = "xyz";
options.Queues =
[
    ["a"] = 1
];
```

Fetches:

```text
TenantId = xyz
Queue = a
```

Does not fetch:

```text
TenantId = abc
Queue = a
TenantId = null, Queue = a
```

### Mixed Mode

Mixed global and tenant-aware servers can run against the same storage only if the storage supports tenant-aware queues and fetch predicates are strict.

Behavior:

- Global servers process only global jobs.
- Tenant servers process only their tenant jobs.
- No server processes both global and tenant jobs unless explicitly configured to do so in a future extension.
- Tenant servers process exactly one tenant in the first implementation. A multi-tenant worker process would require a separate fairness, authorization, resource accounting, and dashboard design.

## Queue Priority

### Current Situation

Queue priority behavior is storage-specific and not consistently expressed as a first-class API guarantee.

In the SQL Server implementation, dequeue queries filter by:

```sql
where Queue in @queues
```

and use `top (1)` without an explicit `order by` in the queue selection query. This means SQL Server does not provide a clear deterministic priority based on the order of `options.Queues`. Observed behavior may appear influenced by indexes, query plans, insertion order, or queue names, but it should not be treated as a stable priority contract.

Some Hangfire documentation or older behavior may imply queue order or alphabetical behavior for specific storages. A multitenant queue design should not depend on ambiguous ordering.

### Priority Requirement

Queue priority must be solved before, or as part of, tenant-aware queues. The new queue declaration is priority-aware instead of an ordered string array.

Target server configuration:

```csharp
options.TenantId = "xyz";
options.Queues =
[
    ["critical"] = 1,
    ["default"] = 2,
    ["bulk"] = 3
];
```

Priority semantics:

- Lower priority number means higher priority.
- Priority `1` is the highest priority.
- Priority values must be positive integers.
- Priority is strict: a higher-priority queue is checked before a lower-priority queue.
- If priorities are equal, use FIFO by queue row id as the deterministic tie-breaker.
- Hangfire does not attempt automatic fairness across priority levels in the first implementation.
- Developers are responsible for choosing queue depth, worker count, and priority values that do not starve lower-priority work.
- Because `Queues` changes from `string[]` to a dictionary-like type, this belongs in a major-version change.

### SQL Fetch Direction

There are two implementation strategies.

Strategy 1: ordered fetch per queue.

- Try queues in priority order.
- Fetch one job from the first queue with available work.
- More round trips, simpler semantics.

Strategy 2: single query with queue priority table.

- Pass queue priorities to SQL.
- Join queue rows with priority values.
- Order by priority, then queue row id.

Example conceptual ordering:

```sql
order by QueuePriority asc, Id asc
```

Strategy 2 is more efficient but more complex and storage-specific.

### Recommendation

Implement deterministic queue priority first in global mode, then apply the same queue descriptor model to tenant-aware fetch. This keeps tenant-aware queues from inheriting ambiguous queue ordering semantics.

The effective SQL ordering for all priority-aware fetches should be:

```sql
order by QueuePriority asc, Id asc
```

For tenant-aware fetch, the tenant predicate is applied before the same priority ordering:

```sql
where TenantId = @tenantId
order by QueuePriority asc, Id asc
```

## Dashboard Requirements

### Queues Page

The Queues page should show tenant scope when multitenant queues are present.

Example:

```text
Tenant  Queue  Enqueued  Fetched  Servers
xyz     a      120       4        2
abc     a      40        1        1
global  a      8         0        1
```

The dashboard should distinguish:

- Global queues.
- Tenant queues.
- Same queue name under different tenants.

### Servers Page

Server metadata should include tenant id:

```text
Server          Tenant  Queues
worker-xyz-01   xyz     a, emails
worker-abc-01   abc     a, emails
worker-global   global  default
```

### Job Details Page

Job details should show tenant id when present:

```text
Tenant: xyz
Queue: a
```

### Dashboard Filtering

Dashboard tenant filter:

```text
All tenants
Global
xyz
abc
```

The first version must include tenant-aware dashboard authorization hooks. The default implementation can preserve current behavior where authorized dashboard users can see all tenants, but applications must be able to narrow dashboard visibility by tenant from the first phase.

Candidate API:

```csharp
public interface IDashboardTenantAuthorizationFilter
{
    bool AuthorizeTenant(DashboardContext context, string tenantId);
}
```

Required behavior:

- Dashboard pages that list tenant-scoped data must apply tenant authorization before rendering tenant rows.
- `tenantId == null` represents global data and must be authorizable separately from named tenants.
- The tenant filter dropdown must include only tenants the current dashboard user is authorized to see.
- Direct links to tenant-scoped queue, job, recurring job, or server views must enforce the same authorization check.
- If no tenant authorization filter is registered, existing dashboard authorization semantics apply and authorized dashboard users can see all tenants.

## Monitoring API Requirements

Existing DTOs can be extended with optional tenant fields.

Queue DTO:

```csharp
public sealed class QueueWithTopEnqueuedJobsDto
{
    public string TenantId { get; set; }
    public string Name { get; set; }
}
```

Server DTO:

```csharp
public sealed class ServerDto
{
    public string TenantId { get; set; }
    public IDictionary<string, int> Queues { get; set; }
}
```

Job DTOs can expose tenant id through properties or state data.

Compatibility:

- Missing `TenantId` means global/legacy.
- Older clients ignore new fields.
- New clients tolerate missing fields.

## Resource Awareness Integration

Tenant-aware queues should compose with resource awareness.

Server resource state applies to the tenant-aware server instance:

```text
Server: worker-01
Tenant: xyz
Queues: a, emails
AllocationState: Available
```

Queue-level resource policies should apply to logical queues within the server's tenant scope:

```text
Tenant xyz, Queue a paused
Tenant abc, Queue a unaffected
```

Dashboard queue availability should include tenant:

```text
Tenant xyz, Queue a: 1 available, 1 constrained
Tenant abc, Queue a: 2 available, 0 constrained
```

Remote drain controls should target the server instance. Per-queue drain should apply to the tenant-scoped queue on that server.

## Recurring Jobs

Recurring jobs are not queue rows until they are enqueued. Tenant behavior must be defined for recurring job creation and enqueue time.

Required behavior:

1. Store tenant id as part of recurring job metadata.
2. Use current tenant context when recurring job is registered only when the API explicitly allows tenant context.
3. Prefer explicit tenant id for tenant-aware recurring jobs in infrastructure code.
4. Scope recurring job identity by `TenantId + RecurringJobId` when tenant id is present.

Recommended direction:

```csharp
RecurringJob
    .ForTenant("xyz")
    .AddOrUpdate("daily-report", () => GenerateReport(), Cron.Daily);
```

Recurring job id uniqueness:

- Global recurring job identity remains `TenantId = null, RecurringJobId = daily-report`.
- Tenant recurring job identity is `TenantId = xyz, RecurringJobId = daily-report`.
- Tenant `xyz` can have recurring job `daily-report`, and tenant `abc` can also have recurring job `daily-report`.
- Dashboard and monitoring APIs must display both the tenant id and recurring job id to avoid apparent duplicates.

Tenant-scoped recurring jobs are part of the first multitenant queue implementation, not a later add-on. Enqueued executions produced by a tenant-scoped recurring job must preserve the recurring job's tenant id and target the tenant-scoped queue selected by filters or queue attributes.

## Batches, Continuations, Retries, And Scheduled Jobs

### Scheduled Jobs

Scheduled jobs should preserve tenant id until they are enqueued.

When a scheduled tenant-aware job becomes enqueued:

```text
TenantId = original tenant id
Queue = target queue
```

### Retries

Retries should preserve original tenant id.

If a tenant-aware job fails and is retried, it must return to the same tenant-scoped queue unless explicitly changed by filters.

### Continuations

Continuations should inherit tenant id by default from the parent job unless the caller explicitly overrides it.

Open question: whether cross-tenant continuations should be allowed. For the first version, avoid special restrictions and preserve explicit caller behavior.

### Batches

Batch behavior depends on Hangfire edition/features and should be designed separately. The default expectation is that batch jobs preserve their tenant id like other jobs.

## Authorization And Isolation

This specification focuses on queue routing, not full tenant security.

Important distinction:

- Tenant-aware queues prevent workers from fetching another tenant's queue jobs.
- They do not automatically prevent dashboard users from seeing all tenants.
- They do not automatically isolate storage tables or encrypted data.

Full tenant isolation would require:

- tenant-aware dashboard authorization
- tenant-aware monitoring filters
- tenant-aware recurring job ids
- tenant-aware counters/statistics
- tenant-aware distributed locks where appropriate
- tenant-aware deletion and cleanup operations

The first implementation includes dashboard authorization hooks, but it does not claim complete tenant security isolation across every storage entity. Strict isolation remains a broader product and storage design.

## Failure Modes

### Tenant-Aware Server With Unsupported Storage

If `BackgroundJobServerOptions.TenantId` is configured but storage does not support tenant-aware queues:

- Server startup should fail fast with a clear exception, or
- Worker fetch should fail with a clear `NotSupportedException`.

Recommended behavior: fail fast during server startup when possible.

### Tenant-Aware Enqueue With Unsupported Storage

If tenant id is provided during enqueue but storage does not support tenant-aware queues:

- Job creation should fail with a clear exception.
- It must not silently enqueue into a global queue.

### Missing Tenant Context

If multitenant mode is expected by the application but no tenant id is present:

- Default behavior remains global enqueue.
- Applications that require tenants can add a client filter that rejects missing tenant ids.

Hangfire should not globally require tenant ids unless configured to do so.

### Tenant Id Mismatch

If job metadata says tenant `xyz` but queue storage says tenant `abc`, storage should treat queue storage as authoritative for fetch. Dashboard should expose mismatch if detected.

The implementation should avoid creating mismatches by centralizing tenant assignment during state application.

### Stale Servers

Tenant-aware server records should be removed by existing server timeout cleanup behavior. Dashboard queue availability should ignore stale tenant-aware servers.

## Migration Strategy

### Existing Users

No action required. Existing behavior remains:

```text
TenantId = null
Queue = existing queue
```

### Users With Tenant-Encoded Queue Names

Applications currently using names like:

```text
xyz_a
abc_a
```

can migrate gradually:

1. Add tenant-aware enqueue for new jobs.
2. Start tenant-aware servers for the new tenant queues.
3. Keep legacy servers for old encoded queues until drained.
4. Remove encoded queue usage after old jobs finish.

Migration tooling can be considered later to rewrite queued jobs, but it is not required for the first version.

### SQL Schema

If using the tenant-column model, SQL migration must be explicit and documented.

Storages should expose granular capability flags rather than a single broad multitenancy flag:

```csharp
JobStorageFeatures.TenantAwareQueueEnqueue
JobStorageFeatures.TenantAwareQueueFetch
JobStorageFeatures.TenantAwareQueueMonitoring
JobStorageFeatures.TenantAwareRecurringJobs
JobStorageFeatures.TenantAwareDashboard
JobStorageFeatures.PriorityAwareQueues
```

Startup and dashboard behavior:

- Tenant-aware enqueue requires `TenantAwareQueueEnqueue`.
- Tenant-aware servers require `TenantAwareQueueFetch`.
- Tenant-aware queue dashboard and monitoring APIs require `TenantAwareQueueMonitoring`.
- Tenant-scoped recurring jobs require `TenantAwareRecurringJobs`.
- Tenant-filtered dashboard views require `TenantAwareDashboard`.
- Priority-aware server queues require `PriorityAwareQueues`.
- If a requested feature is not supported, startup or the first attempted operation must fail clearly instead of silently falling back to global behavior.

## Implementation Phasing

Recommended phases:

1. Replace `BackgroundJobServerOptions.Queues` with a priority-aware dictionary-like collection.
2. Implement deterministic priority-aware fetch in global mode.
3. Add tenant id model, validation, and tenant context.
4. Persist tenant id in job parameters/state data.
5. Add tenant-aware storage abstraction methods for enqueue and fetch using queue descriptors.
6. Implement SQL Server tenant-aware queue storage using nullable `TenantId`.
7. Add `BackgroundJobServerOptions.TenantId` and worker fetch integration for exactly one tenant per server.
8. Add client-side tenant enqueue APIs.
9. Add tenant-scoped recurring job identity and recurring enqueue behavior.
10. Extend scheduled jobs, continuations, and retries for tenant preservation.
11. Extend server metadata and monitoring DTOs with tenant id and queue priorities.
12. Add ASP.NET Core `ITenantIdProvider` convenience integration.
13. Update dashboard Queues, Servers, Recurring Jobs, and Job Details pages.
14. Add tenant-aware dashboard authorization hooks and filtering.
15. Add resource-awareness integration for tenant queue availability.

## Test Plan

### Core Tests

- Default `BackgroundJobServerOptions.TenantId` is null.
- Queue priority collection accepts valid queue names and positive priority values.
- Queue priority collection rejects zero and negative priorities.
- Queue priority collection rejects duplicate queue names.
- Tenant id validation rejects null where required, empty values, whitespace, and invalid characters.
- Tenant context flows across async calls.
- Tenant context restores previous tenant after disposal.
- Tenant-aware jobs persist tenant id in metadata.
- ASP.NET Core `ITenantIdProvider` output is used by convenience APIs when no explicit tenant id is provided.
- Explicit tenant APIs override provider-derived tenant ids.
- Global jobs continue to omit tenant id.
- Tenant-aware workers call tenant-aware fetch methods.
- Global workers use priority-aware fetch while preserving global-only job visibility.
- Tenant-aware worker does not fetch global jobs.
- Global worker does not fetch tenant-aware jobs.

### SQL Server Tests

- Priority-aware global fetch returns higher-priority queues before lower-priority queues.
- Priority-aware fetch uses FIFO by queue row id when priority values are equal.
- Tenant-aware enqueue writes `TenantId`.
- Global enqueue writes null `TenantId`.
- Tenant-aware fetch only returns jobs for matching tenant and queue.
- Tenant-aware fetch respects queue priority within the selected tenant.
- Tenant-aware fetch does not return same queue name for another tenant.
- Global fetch does not return tenant-aware jobs.
- Existing rows with null tenant remain fetchable by global servers.
- Monitoring returns separate queue rows for same queue name under different tenants.
- SQL migration is not required for legacy mode.
- Startup fails clearly when tenant-aware mode is enabled against storage without required schema/capability.

### Dashboard Tests

- Queues page displays tenant column when tenant queues exist.
- Queues page displays queue priority.
- Same queue name appears separately for different tenants.
- Servers page displays tenant id.
- Servers page displays queue priority configuration.
- Job details page displays tenant id for tenant-aware jobs.
- Global jobs display as global or blank tenant.
- Dashboard handles mixed global and tenant queues.
- Dashboard tenant authorization filters unauthorized tenant rows.
- Dashboard tenant filter lists only authorized tenants.
- Direct dashboard links to tenant-scoped data enforce the same tenant authorization check.

### Resource Awareness Tests

- Queue availability is calculated per tenant and queue.
- Tenant `xyz` queue `a` resource constraint does not affect tenant `abc` queue `a`.
- Per-queue drain applies only within the server's tenant scope.
- Remote drain still targets server instance regardless of tenant.

### Recurring/Scheduled/Retry Tests

- Tenant `xyz` and tenant `abc` can both define recurring job id `daily-report`.
- Tenant-scoped recurring job id does not collide with global recurring job id of the same name.
- Scheduled tenant-aware job preserves tenant id when enqueued.
- Retry preserves tenant id.
- Continuation inherits tenant id by default.
- Tenant-aware recurring job enqueues with configured tenant id.

### Compatibility Tests

- Existing queue tests pass unchanged in global mode.
- Existing monitoring clients tolerate missing tenant id.
- Older server metadata without tenant id is treated as global.
- Tenant-aware APIs fail clearly on unsupported storage.
- Storage feature flags are granular for enqueue, fetch, monitoring, recurring jobs, dashboard support, and priority-aware queues.
- Unsupported partial storage capability fails clearly for the requested feature without silently falling back to global behavior.

## Resolved Design Decisions

- Multitenancy remains opt-in. `TenantId == null` keeps the global queue namespace.
- A server can process exactly one tenant in the first implementation.
- Global servers process only global jobs. Tenant-aware servers process only jobs for their configured tenant.
- `BackgroundJobServerOptions.Queues` changes from `string[]` to a priority-aware dictionary-like collection.
- Queue priority is implemented before, or as part of, tenant-aware queues.
- Priority `1` is the highest priority, larger numbers are lower priority, and priority values must be positive integers.
- Priority is strict. If two queues have the same priority, FIFO by queue row id is the deterministic tie-breaker.
- Developers are responsible for queue depth, worker count, and starvation risk.
- Recurring jobs are tenant-scoped in the first multitenant implementation.
- Recurring job identity is `TenantId + RecurringJobId` when tenant id is present.
- Production multitenant queue storage uses a real nullable `TenantId` column.
- Encoded physical queue names are prototype-only and are not the public storage contract.
- ASP.NET Core integration includes an `ITenantIdProvider` convenience abstraction.
- Core Hangfire remains based on explicit tenant APIs and tenant context, not ASP.NET Core request abstractions.
- Dashboard tenant authorization hooks are included in phase one.
- The default dashboard behavior may still show all tenants to authorized dashboard users when no tenant authorization filter is registered.
- Wildcard tenant servers are not supported.
- Storage support is advertised through granular feature flags for enqueue, fetch, monitoring, recurring jobs, dashboard support, and priority-aware queues.

## Remaining Open Questions

None.
