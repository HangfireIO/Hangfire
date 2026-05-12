# Resource Awareness Operational Controls Specification

## Summary

The current resource-awareness work lets each `BackgroundJobServer` decide whether it can fetch more jobs, publish allocation state through server metadata, expose that state in monitoring, and show it in the dashboard. This specification describes the next operational layer: dashboard-driven drain controls, resource history, queue availability summaries, Kubernetes shutdown integration, dashboard alerts, stronger built-in probes, and per-queue drain mode.

The goal is to make resource awareness useful during real operations: deployments, maintenance, incidents, queue stalls, and platform shutdown. These features should preserve Hangfire's pull-based worker model. Servers still fetch jobs from queues, but operators and hosting integrations get better ways to pause intake, understand why work is not moving, and safely wind down nodes.

## Goals

- Allow authorized dashboard users to remotely request drain and resume for a live server.
- Record short-lived resource state history that explains how long a server has been constrained or draining.
- Show queue-level availability so operators can see whether a queue has any servers able to process it.
- Provide first-class ASP.NET Core and Kubernetes-friendly shutdown behavior.
- Surface actionable dashboard alerts for resource-related operational problems.
- Improve built-in CPU and memory probes so common resource checks work without custom code.
- Support per-queue drain controls for targeted operational pauses.
- Avoid SQL schema migrations for the first version of each feature where existing storage primitives are sufficient.
- Keep existing deployments compatible and keep resource awareness opt-in.

## Non-Goals

- Do not replace Hangfire's existing pull-based worker model with a central scheduler.
- Do not introduce hard global resource slot accounting.
- Do not cancel or interrupt jobs that have already been fetched solely because a drain command or resource constraint appears.
- Do not implement job-level resource requirements such as `[RequiresResource("gpu")]`.
- Do not require Kubernetes or ASP.NET Core for core resource-awareness behavior.
- Do not require all storage providers to implement advanced history or commands immediately; unsupported storages should degrade safely.

## Terminology

- **Allocation**: Whether a server is allowed to fetch and reserve new jobs.
- **Drain**: An intentional operational state where a server stops fetching new jobs while allowing already fetched jobs to finish.
- **Remote drain**: A drain requested through shared storage, typically from the dashboard, and later observed by the target server.
- **Per-queue drain**: A drain that applies only to one or more queues on a server.
- **Resource history**: Short-lived events or counters describing transitions between resource states.
- **Queue availability**: The number of live servers that can currently fetch from a given queue.

## 1. Remote Drain Controls From The Dashboard

### Requirement

The dashboard should allow an authorized user to request drain or resume for a specific live server from the Servers page.

The controls should be available only when:

- The server is still considered alive according to heartbeat rules.
- The current dashboard user is authorized to perform command actions.
- The storage provider supports resource commands.
- The server version is expected to understand resource commands, or the UI can clearly show that support is unknown.

### User Experience

The Servers page should show per-server actions:

- `Drain`: request that the server stop fetching new jobs.
- `Resume`: request that the server accept new jobs again.

The `Drain` action should prompt for an optional reason.

Recommended wording:

```text
Drain server

Stop this server from fetching new jobs. Already processing jobs will continue.

Reason:
[ Deployment in progress ]

[Drain server] [Cancel]
```

The UI must avoid wording like `Stop server`, `Kill`, or `Abort`, because drain mode does not terminate the process or cancel jobs.

### States

The dashboard should distinguish command state from observed server state:

- `Drain requested`: a drain command exists in storage, but the server has not yet published `Draining`.
- `Draining`: the server has observed the command and published drain state.
- `Resume requested`: a resume command exists, but the server has not yet published `Available` or another non-drain state.
- `Available`: the server can fetch jobs.
- `Resource constrained`: the server is alive but unable to allocate due to resource pressure.
- `Offline`: the server has missed heartbeat.
- `Unknown`: resource metadata is missing or incompatible.

### Storage Model

Remote commands should be stored using existing storage primitives. For SQL Server this can use existing hash/set tables and should not require a schema migration.

Suggested command key:

```text
server:{serverId}:resource-command
```

Suggested fields:

```text
command = drain | resume
reason = Deployment in progress
createdAt = 2026-05-12T10:20:30.0000000Z
createdBy = user display name or null
commandId = generated id
target = server instance id
```

The command key is scoped to the full Hangfire server id. Since server ids are ephemeral, the first implementation drains the current server instance only. Node-level commands that target all instances on a host are explicitly deferred to a later design, because they can accidentally affect restarted or future workers.

The `createdBy` field should be captured by default on a best-effort basis. ASP.NET Core hosts should use the current `ClaimsPrincipal` identity name when available. OWIN hosts should use the OWIN user identity when available. If no authenticated identity is available, `createdBy` should be null rather than blocking the command.

### Server Behavior

Each resource-aware server should run a lightweight command polling process.

Behavior:

- Poll storage for commands addressed to the current server id.
- If command is `drain`, put the local resource into drain mode and publish updated server metadata.
- If command is `resume`, clear drain mode and publish updated server metadata.
- Continue heartbeating while drained.
- Do not fetch new jobs while drained.
- Allow already fetched jobs to complete normally.
- Ignore unknown commands and log a warning.
- Treat duplicate drain or resume commands as idempotent.
- Observe server shutdown tokens so command polling does not delay shutdown.

### API Direction

Core resource API:

```csharp
public interface IJobServerDrainController
{
    void Drain(string reason);
    void Resume();
}
```

Storage command API direction:

```csharp
public interface IServerResourceCommandStorage
{
    ServerResourceCommand GetCommand(string serverId);
    void SaveCommand(string serverId, ServerResourceCommand command);
    void ClearCommand(string serverId, string commandId);
}
```

The exact implementation can use existing `JobStorageConnection` virtual methods instead of introducing a new public interface if that better matches Hangfire storage patterns.

### Compatibility

- Older servers that do not support commands should ignore command records.
- New dashboards should not assume every server supports remote drain.
- Missing command support should hide or disable command buttons with a short tooltip.
- A command addressed to an offline server should be shown as stale and eventually cleaned up.

### Security

Remote drain and resume are operational commands. They require authorization that is separate from normal dashboard read authorization.

Reading the dashboard and mutating server intake are different privileges. The design should add a dedicated command authorization capability for server resource commands. Deployments that want the old all-or-nothing behavior can configure the command authorization policy to reuse their existing dashboard authorization.

## 2. Resource History Timeline

### Requirement

Hangfire should record short-lived resource events that explain state transitions and durations.

Examples:

- Server entered `Resource constrained` because of `Memory pressure`.
- Server entered `Draining` because of `Deployment in progress`.
- Server resumed allocation.
- Capacity check failed.
- Queue `image-processing` was paused due to `CPU pressure`.
- Queue `image-processing` resumed.

### Data Model

Suggested event model:

```csharp
public sealed class ServerResourceEvent
{
    public string ServerId { get; set; }
    public string EventType { get; set; }
    public string AllocationState { get; set; }
    public string Queue { get; set; }
    public string Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; }
}
```

Event types:

- `allocation-state-changed`
- `drain-requested`
- `resume-requested`
- `capacity-check-failed`
- `queue-state-changed`
- `command-observed`
- `command-failed`

### Storage Direction

The first implementation should prefer existing lists, sets, counters, or hashes.

Suggested keys:

```text
resource-events:server:{serverId}
resource-events:queue:{queue}
resource-events:recent
```

Retention:

- Default retention should be 7 days.
- Retention should be configurable.
- Events should expire automatically where storage supports expiration.

Counters can complement events:

```text
stats:resource:capacity-check-failed
stats:resource:fetch-skipped
stats:resource:drain-requested
stats:resource:queue-paused
```

### Dashboard

The Servers page should show compact duration text:

```text
Draining for 18 minutes
Resource constrained for 4 minutes
Last check failed 2 minutes ago
```

A server details section can show recent events:

```text
10:05 Draining - Deployment in progress
10:03 Drain requested - user@example.com
09:51 Available
09:44 Resource constrained - Memory pressure
```

### Duration Calculation

Duration can be derived from:

- Current server metadata, such as allocation checked time or state changed time.
- Most recent matching resource event.

If event history is unavailable, the dashboard should still show current state and checked time.

### Monitoring API

Resource events should be exposed through public monitoring APIs, not kept as dashboard-internal data. Dashboard-only history is easier to implement, but public monitoring APIs make the same data available to external operations dashboards, alerting systems, and support tooling.

Candidate API:

```csharp
IList<ServerResourceEvent> ResourceEvents(string serverId, int from, int count);

IList<ServerResourceEvent> ResourceEvents(DateTime from, DateTime to);
```

The exact shape can be adjusted during implementation, but the first version should treat resource events as a monitoring surface rather than private dashboard state.

### Compatibility

- Missing history support should not break server metadata or dashboard rendering.
- The dashboard should hide timeline sections when the storage provider does not support event history.
- Event history should be best effort; failure to record history must not stop workers.

## 3. Queue Availability Summary

### Requirement

The Queues page should show how many live servers can currently process each queue.

Example:

```text
image-processing: 1 available, 3 constrained
emails: 4 available, 0 constrained
```

This helps operators diagnose queue stalls where jobs are enqueued but all matching servers are drained, constrained, offline, or queue-paused.

### Availability Rules

A server is available for a queue when:

- The server heartbeat is valid.
- The server has the queue in its configured queue list.
- Global allocation state is available.
- The server is not draining globally.
- Queue-level allocation for that queue is available or missing.

A server is constrained for a queue when:

- The server heartbeat is valid.
- The server has the queue in its configured queue list.
- The server cannot fetch from the queue because of global resource pressure, global drain, queue-level drain, or queue-level resource pressure.

Offline servers should be counted separately or excluded, but not counted as available.

### Monitoring Model

Possible DTO:

```csharp
public sealed class QueueAvailabilityDto
{
    public string Queue { get; set; }
    public int AvailableServers { get; set; }
    public int ConstrainedServers { get; set; }
    public int DrainingServers { get; set; }
    public int OfflineServers { get; set; }
    public IDictionary<string, int> ConstrainedByReason { get; set; }
}
```

Possible monitoring API:

```csharp
IList<QueueAvailabilityDto> QueueAvailability();
```

Queue availability should be exposed through monitoring APIs. The dashboard can compute a temporary prototype from `Servers()`, but the supported design should provide an API so external tooling can use the same data and storage providers can optimize the calculation.

### Dashboard

The Queues page should add a small availability column or expandable detail:

```text
Queue              Enqueued  Fetched  Servers
image-processing   120       8        1 available, 3 constrained
emails             4         0        4 available
```

If all servers for a queue are constrained, show a warning badge:

```text
No available servers
```

Clicking or expanding should list reasons:

```text
2 draining
1 CPU pressure
1 Memory pressure
```

### Compatibility

- Older server records without resource metadata should be treated as available if heartbeat is valid.
- Storages that do not expose resource metadata should show current queue information without availability details.

## 4. First-Class Kubernetes Shutdown Integration

### Requirement

ASP.NET Core hosting should provide a helper that enters drain mode when application shutdown begins, waits for in-flight jobs when possible, and publishes `Draining` state before the process exits.

This is primarily useful for Kubernetes pod termination, rolling deployments, blue/green deployments, and cloud orchestrators that send termination signals.

### Behavior

When `IHostApplicationLifetime.ApplicationStopping` fires:

- The server enters drain mode with a reason such as `Application stopping`.
- Workers stop fetching new jobs.
- Already fetched jobs continue until normal server shutdown rules stop them.
- Server metadata is updated to show `Draining`.
- The server continues heartbeating during graceful shutdown where existing Hangfire lifecycle permits.
- The helper waits up to a configured timeout for processing jobs to finish, if the necessary processing tracking is available.

### API Direction

ASP.NET Core extension:

```csharp
services.AddHangfireServer(options =>
{
    options.Resource = resource;
});

services.AddHangfireResourceAwareShutdown(options =>
{
    options.Resource = resource;
    options.Reason = "Kubernetes pod terminating";
    options.GracefulShutdownTimeout = TimeSpan.FromSeconds(25);
});
```

Alternative option-based design:

```csharp
services.AddHangfireServer(options =>
{
    options.Resource = resource;
    options.DrainOnApplicationStopping = true;
    options.DrainShutdownTimeout = TimeSpan.FromSeconds(25);
});
```

Recommended direction: implement the hosting integration in `Hangfire.NetCore`, because the host lifetime abstraction is part of .NET hosting rather than HTTP or MVC. `Hangfire.AspNetCore` can provide convenience wiring that delegates to the `Hangfire.NetCore` implementation.

### Kubernetes Guidance

Documentation should include a recommended pod lifecycle:

```yaml
terminationGracePeriodSeconds: 60
```

And explain:

- Hangfire should enter drain mode before the pod exits.
- The grace period must be longer than typical job completion time, or jobs may still be requeued according to existing Hangfire behavior.
- Drain mode prevents new fetches but does not guarantee every in-flight job finishes before Kubernetes kills the container.

### Observability

Dashboard should show:

- `Draining`
- reason: `Kubernetes pod terminating` or configured reason
- time since drain started

### Compatibility

- This feature should be optional.
- Non-ASP.NET Core hosts should not depend on it.
- Applications without `JobServerResource` configured should keep existing shutdown behavior.

## 5. Resource-Aware Dashboard Alerts

### Requirement

The dashboard should show lightweight alerts for common resource-awareness problems.

Initial alerts:

- All servers for a queue are constrained.
- A server has been draining longer than a configurable threshold.
- A server has been resource constrained longer than a configurable threshold.
- Capacity checks are repeatedly failing.
- A remote drain command is pending but not yet observed.

### Alert Model

Suggested internal model:

```csharp
public sealed class ResourceAlert
{
    public string Severity { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string ServerId { get; set; }
    public string Queue { get; set; }
    public string Reason { get; set; }
}
```

Severity values:

- `Info`
- `Warning`
- `Critical`

### Dashboard Placement

Servers page:

- Long drain warnings.
- Capacity check failure warnings.
- Pending command warnings.

Queues page:

- All servers constrained for a queue.
- No live server can process a queue.

Dashboard home page:

- Compact summary count of resource warnings.

### Configuration

Dashboard options should expose thresholds:

```csharp
public TimeSpan ResourceConstrainedWarningThreshold { get; set; }
public TimeSpan DrainWarningThreshold { get; set; }
public int CapacityCheckFailureWarningThreshold { get; set; }
```

Defaults should be conservative:

- Do not warn immediately when a server drains.
- Do warn when all servers for a queue are constrained.
- Do warn after repeated capacity check failures.

### Compatibility

- Alerts are derived from available monitoring data.
- If history is unavailable, alerts that require duration or repeated failure counts should be hidden or degraded to current-state warnings.

## 6. Better Built-In Resource Probes

### Requirement

Built-in resource providers should be useful for common environments without requiring applications to write custom probes.

Priority probes:

- CPU utilization.
- Process memory usage.
- System memory pressure.
- Disk free space.
- Composite checks.

### CPU Provider

Candidate API:

```csharp
JobServerResource.FromCpuLoad(
    maxCpuLoad: 0.85,
    interval: TimeSpan.FromSeconds(5),
    options =>
    {
        options.Window = TimeSpan.FromSeconds(30);
        options.FailClosedWhenUnsupported = false;
    });
```

Behavior:

- Report unavailable when CPU load exceeds threshold.
- Use a rolling window to avoid flapping.
- Include reason `CPU pressure`.
- Include checked timestamp.
- If CPU load is unsupported on a platform, report unsupported reason.
- Fail closed only when explicitly configured.

### Memory Provider

Candidate APIs:

```csharp
JobServerResource.FromProcessMemoryLimit(long maxBytes, TimeSpan interval);

JobServerResource.FromSystemMemoryPressure(
    double maxUsedRatio,
    TimeSpan interval,
    bool failClosedWhenUnsupported = false);
```

Behavior:

- Process memory checks should work wherever runtime APIs expose process working set or private bytes.
- System memory checks should use platform-supported APIs where available.
- Unsupported system memory metrics should not surprise users by failing closed unless configured.

### Disk Provider

Candidate API:

```csharp
JobServerResource.FromDiskFreeSpace(
    path: "/data",
    minFreeBytes: 10L * 1024 * 1024 * 1024,
    interval: TimeSpan.FromSeconds(30));
```

Behavior:

- Report unavailable when available free space is below threshold.
- Include reason `Disk pressure`.
- If path or drive is unavailable, report unavailable with reason `Disk metric unavailable` or fail according to options.

### Composite Provider

Candidate API:

```csharp
JobServerResource.FromComposite(
    TimeSpan.FromSeconds(5),
    cpu,
    memory,
    disk);
```

Behavior:

- Composite is unavailable when any required child is unavailable.
- Reason should identify the first failing provider or combine concise reasons.
- Checked timestamp should reflect the composite computation time.

### Platform Notes

Hangfire targets older .NET Framework and .NET Standard versions. Implementations must avoid raising target framework requirements.

Recommended strategy:

- Use conditional compilation for richer APIs.
- Use reflection only when it is simple and reliable.
- Provide clear unsupported reasons.
- Keep custom provider support as the escape hatch.

Built-in probes should cover common, safe cases only:

- CPU load where a reliable runtime or platform API is available.
- Process memory usage.
- System memory pressure where reliable platform APIs are available.
- Disk free space.
- Composite checks.

Specialized or environment-specific probes, such as GPU pressure, per-container cgroup details beyond commonly supported APIs, external dependency health, tenant-specific limits, or cloud-provider-specific metrics, should remain custom provider territory.

## 7. Per-Queue Drain

### Requirement

Operators and applications should be able to drain one queue on a server while allowing the same server to continue fetching from other queues.

Example:

```csharp
resource.DrainQueue("image-processing", "GPU maintenance");
resource.ResumeQueue("image-processing");
```

### Behavior

- A drained queue is removed from the queue list passed to `FetchNextJob`.
- If at least one configured queue remains available, the worker fetches from the remaining queues.
- If all configured queues are drained or constrained, the worker waits using the existing resource polling behavior.
- Global drain still overrides per-queue availability and prevents all fetching.
- Already fetched jobs from the drained queue continue normally.

### Dashboard

The Servers page should show paused queues inline:

```text
Queues: default, image-processing paused
Reason: GPU maintenance
```

The Queues page should include per-queue drain in availability summaries:

```text
image-processing: 0 available, 2 queue-drained, 1 offline
```

### Remote Per-Queue Drain

Remote command model should support optional queue scope:

```text
command = drain-queue | resume-queue
queue = image-processing
reason = GPU maintenance
```

Commands:

- `drain-queue`
- `resume-queue`
- `drain`
- `resume`

### API Direction

```csharp
public interface IJobServerQueueDrainController
{
    void DrainQueue(string queue, string reason);
    void ResumeQueue(string queue);
}
```

`JobServerResource` can implement this directly if it already owns queue allocation state.

### Compatibility

- Existing workers without queue resource support continue to fetch all configured queues.
- Storages do not need to change `FetchNextJob`; workers filter queues before calling storage.
- Older dashboard clients should ignore additional queue metadata.

## 8. Storage And Monitoring Model

### Server Metadata

Current server metadata should continue to carry current state:

- `CanAllocate`
- `AllocationState`
- `AllocationReason`
- `AllocationCheckedAt`
- `DrainMode`
- `QueueAllocation`

Additional optional fields:

- `AllocationStateChangedAt`
- `DrainStartedAt`
- `LastCapacityCheckFailedAt`
- `CapacityCheckFailureCount`
- `RemoteCommandState`

### Commands

Commands should use existing storage primitives and expire or be cleaned up.

Required command fields:

- command id
- command name
- server id
- optional queue
- optional reason
- created at
- created by, when available

### History

History should be optional and best effort.

Event data should support:

- recent events per server
- recent events per queue
- dashboard-wide recent events
- expiration

### Monitoring DTOs

Possible additions:

```csharp
public sealed class ServerDto
{
    public string RemoteCommandState { get; set; }
    public DateTime? AllocationStateChangedAt { get; set; }
    public DateTime? DrainStartedAt { get; set; }
    public DateTime? LastCapacityCheckFailedAt { get; set; }
    public long CapacityCheckFailureCount { get; set; }
}
```

New DTOs can be introduced for queue availability and resource events if dashboard-only computation becomes too expensive or storage-specific.

## 9. Failure Modes

### Command Write Failure

- Dashboard should show an error.
- No server behavior should change.
- The error should be logged.

### Command Poll Failure

- Server should log a warning.
- Existing local allocation state should remain unchanged.
- Workers should not crash solely because command polling failed.

### Stale Drain Command

- If the target server is offline, the dashboard should show the command as stale.
- Cleanup can remove commands for servers that no longer exist after a retention period.

### Capacity History Write Failure

- History write failures should be logged at warning or debug level.
- Current allocation state should still control worker fetching.
- Failure to write history must not stop job processing.

### Unsupported Probe Metric

- Provider should publish an unsupported reason.
- Provider should fail open by default unless configured to fail closed.
- Dashboard should show unsupported as an explanatory reason, not as a server failure.

### Kubernetes Grace Period Expires

- Jobs still running when the platform terminates the process follow existing Hangfire guarantees.
- Drain mode reduces new fetches but does not guarantee every job finishes before external termination.

## 10. Implementation Phasing

Recommended order:

1. Add storage-backed server resource commands for `drain` and `resume`.
2. Add server command polling process and metadata updates.
3. Add dashboard controls for per-server drain and resume.
4. Add queue availability summary computed from existing `Servers()` metadata.
5. Add per-queue drain API and local worker filtering.
6. Extend remote commands to support `drain-queue` and `resume-queue`.
7. Add short-lived resource history events and duration display.
8. Add dashboard alerts based on current state and history.
9. Add `Hangfire.NetCore` Kubernetes-friendly shutdown integration, plus `Hangfire.AspNetCore` convenience wiring.
10. Improve built-in CPU and memory probes with platform-specific implementations.

## 11. Test Plan

### Core Tests

- Remote drain command causes a server resource to enter drain mode.
- Remote resume command causes a drained resource to resume allocation.
- Duplicate drain and resume commands are idempotent.
- Unknown commands are ignored and logged.
- Workers do not fetch jobs while global drain is active.
- Workers continue processing already fetched jobs after drain begins.
- Per-queue drain filters only the drained queue.
- Workers wait when all configured queues are drained.
- Global drain overrides queue availability.
- Capacity history write failures do not stop workers.

### Storage Tests

- Commands are written using existing storage primitives.
- Commands can be read by server id.
- Commands can be cleared after resume or after being observed.
- Stale commands can be cleaned up.
- Resource history events are stored with expiration where supported.
- SQL Server implementation requires no schema migration.

### Dashboard Tests

- Servers page shows `Drain` for available servers.
- Servers page shows `Resume` for drained servers.
- Dashboard posts drain command with reason.
- Dashboard posts resume command.
- Dashboard distinguishes `Drain requested` from `Draining`.
- Dashboard shows stale commands for offline servers.
- Queues page shows available and constrained server counts.
- Alerts appear when all servers for a queue are constrained.
- Alerts appear when drain exceeds threshold.
- Alerts appear for repeated capacity check failures.

### Hosting Tests

- ASP.NET Core shutdown helper drains on `ApplicationStopping`.
- Shutdown helper publishes drain reason.
- Shutdown helper respects configured timeout.
- Existing applications without the helper retain current shutdown behavior.

### Probe Tests

- CPU provider reports available below threshold.
- CPU provider reports constrained above threshold.
- CPU provider reports unsupported on unsupported platforms.
- CPU provider fail-closed option reports constrained when unsupported.
- Memory provider reports according to threshold.
- Disk provider reports according to free-space threshold.
- Composite provider reports constrained when any required child is constrained.

## 12. Resolved Design Decisions

- Remote drain commands target ephemeral server ids in the first implementation. Node-level commands are deferred to a later design.
- Dashboard command authorization is separate from normal dashboard read authorization.
- Command history includes `createdBy` by default when identity is available. ASP.NET Core and OWIN hosts should capture the current principal identity on a best-effort basis; unauthenticated commands store null.
- Resource events are exposed through public monitoring APIs.
- Resource history retention defaults to 7 days and is configurable.
- Queue availability is exposed through monitoring APIs. Dashboard-only computation is acceptable for prototypes but not the final supported design.
- Kubernetes shutdown integration lives in `Hangfire.NetCore`, with `Hangfire.AspNetCore` convenience wiring.
- Built-in probes cover common, safe cases: CPU, process memory, system memory where reliable, disk free space, and composite checks. Specialized platform probes remain custom-provider territory.
