# Resource Awareness Continuation Specification

## Summary

The first resource-awareness implementation adds a boolean server capacity gate through `IJobServerResource`, `BackgroundJobServerOptions.Resource`, worker fetch checks, and persisted `CanAllocate` server metadata. This continuation builds on that foundation to make resource awareness operationally useful: visible in the dashboard, explainable through reasons, controllable through drain mode, easier to configure with built-in providers, and measurable through monitoring.

The continuation should preserve Hangfire's pull-based worker model. Workers still fetch from queues, but each server can publish richer capacity information and avoid fetching work when the local node should not accept more jobs.

## Goals

- Make server resource state visible and understandable in the dashboard.
- Explain why a server is not currently allocating jobs.
- Support graceful node draining for deployments and maintenance.
- Provide built-in resource providers for common local constraints.
- Allow partial capacity by queue where a server can continue processing safe queues while pausing constrained queues.
- Add monitoring data that can be used for alerts and operational dashboards.
- Keep resource-aware job requirements as a future extension, not part of this continuation's first implementation.

## Non-Goals

- Do not replace Hangfire's pull-based worker model with central scheduling.
- Do not introduce hard global cluster slot accounting in this continuation.
- Do not require a SQL schema migration unless richer history storage is introduced later.
- Do not stop or cancel jobs that have already been fetched only because the node becomes resource constrained.
- Do not implement job-level resource requirements in this phase.

## 1. Dashboard Node Capacity Panel

### Requirement

The dashboard should expose resource awareness directly on the Servers page or through a dedicated server capacity section.

Each server should display:

- Server name.
- Heartbeat status.
- Worker count.
- Queues.
- Current allocation state.
- Last known allocation reason.
- Last capacity check time, when available.

### Allocation States

The UI should display a small, scannable state derived from server metadata:

- `Available`: server can fetch new jobs.
- `Resource constrained`: server is alive but currently not fetching new jobs due to capacity.
- `Draining`: server is alive and intentionally not fetching new jobs.
- `Offline`: server has missed heartbeat according to existing server timeout logic.
- `Unknown`: server metadata is missing or incompatible.

### Behavior

- Older server records without resource metadata should be displayed as `Available` when heartbeat is valid.
- Dashboard behavior must remain compatible with storages that only expose the original `CanAllocate` boolean.
- The UI should avoid implying that a constrained server has failed. It is still healthy if heartbeats continue.

## 2. Named Resource Reasons

### Requirement

Servers should be able to report a human-readable reason when they stop allocating work.

Examples:

- `CPU pressure`
- `Memory pressure`
- `Disk pressure`
- `External dependency unavailable`
- `Manually drained`
- `Capacity check failed`

### Public Model

Introduce a richer resource snapshot model while preserving compatibility with `CanAllocate()`:

```csharp
public sealed class JobServerResourceSnapshot
{
    public bool CanAllocate { get; }
    public string Reason { get; }
    public DateTime? CheckedAt { get; }
}
```

The exact API shape can be adjusted during implementation, but it should support:

- Boolean allocation state for worker behavior.
- Optional reason text for dashboard and monitoring.
- Optional timestamp for freshness checks.

### Compatibility

- Existing implementations of `IJobServerResource.CanAllocate()` must continue to work.
- If a resource provider does not expose a reason, `Reason` should be null or empty.
- Storage and monitoring should treat missing reason fields as unknown, not as an error.

## 3. Graceful Drain Mode

### Requirement

Hangfire should support a built-in drain mode where a server stops fetching new jobs while allowing already fetched jobs to complete normally.

### Behavior

- When drain mode is enabled, workers do not fetch new jobs.
- Already processing jobs continue through existing execution and state transition behavior.
- The server continues heartbeating.
- The server is shown as `Draining` in monitoring and dashboard surfaces.
- Drain mode should not mark the server as failed or offline.

### Use Cases

- Application deployment.
- Kubernetes pod termination.
- VM or node maintenance.
- Blue/green deployment.
- Manual operational pause.

### API Direction

Drain mode can be implemented as either:

- A built-in `JobServerResource` state, for example `resource.Drain("reason")`.
- A separate drain-aware resource wrapper.
- A server control API that updates resource state.

The implementation should prefer the smallest API that fits the existing `JobServerResource` model.

## 4. Built-In Resource Providers

### Requirement

Provide built-in helpers for common resource checks so applications do not need to write custom capacity code for basic scenarios.

### Candidate Providers

Memory threshold:

```csharp
JobServerResource.FromMemoryLimit(...)
```

CPU threshold:

```csharp
JobServerResource.FromCpuLoad(...)
```

Disk free-space threshold:

```csharp
JobServerResource.FromDiskFreeSpace(...)
```

Composite provider:

```csharp
JobServerResource.FromComposite(...)
```

### Composite Behavior

- A composite resource should be unavailable when any required child provider is unavailable.
- The reason should identify the first failing provider or combine multiple concise reasons.
- Provider checks should be cancellation-aware where possible.

### Platform Notes

- CPU and memory implementations must account for target framework and operating system support.
- If a metric is unavailable on a platform, the provider should fail closed only when explicitly configured that way. Otherwise it should report an unsupported reason and avoid surprising behavior.

## 5. Queue-Level Resource Policies

### Requirement

A server should be able to pause fetching from constrained queues while continuing to process other queues.

Example: a node may pause `image-processing` jobs during CPU pressure while continuing to process `emails` and `notifications`.

### Behavior

- Queue-level capacity is evaluated before fetching.
- Workers should only fetch from queues currently allowed by the resource policy.
- If no configured queue is currently available, the worker waits using the existing resource polling behavior.
- Existing global `CanAllocate == false` should still prevent all fetching.

### API Direction

Possible model:

```csharp
public sealed class JobServerQueueResourceSnapshot
{
    public string Queue { get; }
    public bool CanAllocate { get; }
    public string Reason { get; }
}
```

The implementation should preserve compatibility with existing storages and `FetchNextJob(string[] queues, ...)`. The simplest implementation can filter the queue list before fetch.

### Dashboard

The dashboard should show queues that are paused by resource policy, including their reason when available.

## 6. Resource-Aware Job Filters

### Status

Future work. This point should not be implemented as part of the first continuation phase.

### Future Requirement

Allow jobs to declare resource requirements or capabilities that must be matched by a server before execution.

Possible examples:

```csharp
[RequiresResource("gpu")]
[RequiresResource("high-memory")]
[RequiresResource("network:stripe")]
```

### Future Behavior

- Servers advertise capabilities.
- Jobs declare required capabilities.
- Workers only fetch or execute jobs compatible with the local server.
- Dashboard and monitoring expose unmatched jobs and available capability capacity.

### Current Hangfire Behavior

Hangfire currently has a low-level fetched-job requeue mechanism through `IFetchedJob.Requeue()`. A worker uses this when an exception escapes while the fetched queue item still needs to be returned. Some storages also make fetched jobs visible again after timeout or when the worker shuts down before acknowledging completion.

However, the current worker flow does not provide a first-class scheduling decision where a server fetches a job, inspects its requirements, and then says "this node cannot handle this job, let another server take it." Resource awareness currently prevents fetching before `FetchNextJob` is called. Once a job is fetched and moved to `Processing`, the worker is expected to perform it or transition it through the existing state pipeline.

### Future Design Implication

Resource-aware job filters should prefer avoiding incompatible fetches instead of fetching and returning jobs repeatedly. A reject-and-requeue path may still be useful as a fallback, but it should be explicitly designed to avoid hot loops where the same incompatible server keeps fetching the same incompatible job.

Any future reject-and-requeue behavior should define:

- When compatibility is evaluated.
- Whether the job remains in `Enqueued` or temporarily moves to another state.
- How to prevent the same server from immediately refetching the rejected job.
- How rejection reasons are exposed in dashboard and monitoring.
- How storage implementations preserve at-least-once processing guarantees.

### Notes

This feature changes job placement semantics more deeply than server-level capacity. It should be designed separately to avoid coupling the current continuation to a larger scheduler model.

## 7. Metrics And Alerts

### Requirement

Resource awareness should expose operational metrics suitable for dashboards and alerts.

### Candidate Metrics

- Current allocation state per server.
- Duration spent unavailable due to resources.
- Duration spent draining.
- Last successful capacity check time.
- Last failed capacity check time.
- Capacity check failure count.
- Worker fetch skips caused by resource constraints.
- Available server count per queue.
- Constrained server count per reason.
- Percentage of currently available server capacity.

### Storage Direction

The first implementation should prefer existing server metadata for current state. Historical counters can use existing counter or statistics mechanisms where appropriate, but should not require schema changes unless a later design explicitly justifies them.

### Alerting Examples

- All servers for a queue are resource constrained.
- A server has been constrained for longer than a configured threshold.
- Capacity checks are repeatedly failing.
- A server remains in drain mode longer than expected.

## Storage And Monitoring Model

Extend current serialized server metadata with optional fields:

- `CanAllocate`
- `AllocationState`
- `AllocationReason`
- `AllocationCheckedAt`
- `DrainMode`
- `QueueAllocation`

Compatibility rules:

- Missing `CanAllocate` means available, matching the first specification.
- Missing richer fields means unknown or not configured.
- Older dashboard and monitoring clients should continue to function when these fields are present.
- New dashboard and monitoring clients should tolerate older records without these fields.

## Implementation Phasing

Recommended order:

1. Add richer resource snapshot state and metadata persistence.
2. Add graceful drain mode.
3. Update monitoring DTOs and dashboard server display.
4. Add built-in resource providers.
5. Add queue-level resource policies.
6. Add metrics and counters.
7. Design resource-aware job filters as a separate future specification.

## Test Plan

Core tests:

- Existing `IJobServerResource.CanAllocate()` behavior remains compatible.
- A resource reason is exposed in server metadata when configured.
- Drain mode prevents new fetches and allows existing jobs to complete.
- Drain mode is distinguishable from resource pressure.
- Built-in providers return available and unavailable states according to thresholds.
- Composite providers become unavailable when a required child provider is unavailable.
- Queue-level policies filter the queue list before fetching.
- Workers wait when all configured queues are unavailable.

Monitoring and dashboard tests:

- Server DTOs expose allocation state, reason, and checked timestamp.
- Older server metadata without new fields is handled correctly.
- Dashboard displays available, constrained, draining, offline, and unknown states.
- Queue-level constraints are visible when present.

Storage tests:

- SQL Server serializes and deserializes new optional metadata fields.
- Updating resource metadata does not require a schema migration.
- Older serialized server records remain readable.

Metrics tests:

- Fetch skips caused by resource constraints are counted.
- Capacity check failures are counted.
- Drain duration and constrained duration can be derived or reported.

## Open Questions

- Should drain mode be controlled only in-process, or should storage-backed remote drain commands be supported?
- Should built-in CPU and memory providers live in `Hangfire.Core`, or in hosting-specific packages where runtime APIs are richer?
- Should queue-level policy be part of `IJobServerResource`, or a separate interface to avoid complicating the current boolean contract?
- How much historical resource data should Hangfire store by default?
