# Resource-Aware BackgroundJobServer Specification

## Summary

Hangfire currently uses a pull-based worker model: each `BackgroundJobServer` announces itself, starts worker threads, and those workers fetch jobs from shared queues when they are ready. The first version of resource awareness should preserve that model and add an opt-in capacity gate for each server.

When a server reports that it has no capacity, its workers remain alive but stop fetching new jobs. The server continues to heartbeat and remains visible in monitoring. When capacity returns, workers resume fetching jobs normally. Existing deployments keep the current behavior when no resource provider is configured.

## Goals

- Allow applications hosting `BackgroundJobServer` to report whether the local node can accept more jobs.
- Prevent workers on a saturated node from fetching and reserving additional jobs.
- Expose each server's latest capacity state through existing server metadata and monitoring.
- Preserve existing Hangfire behavior by default.
- Avoid a SQL schema migration for the first version.

## Non-Goals

- Do not introduce central push-based job assignment to specific nodes.
- Do not add numeric slot accounting in the first version.
- Do not add named resource dimensions such as CPU, memory, GPU, tenant, or job type in the first version.
- Do not change the existing at-least-once execution guarantees after a job has already been fetched.

## Public API

Add a new public interface in `Hangfire.Core`:

```csharp
public interface IJobServerResource
{
    bool CanAllocate();

    void CapacityReporter(Func<Task<bool>> computeCapacity, TimeSpan interval);
}
```

Add a nullable property to `BackgroundJobServerOptions`:

```csharp
public IJobServerResource Resource { get; set; }
```

Default behavior:

- `Resource == null` means the server is always available.
- Capacity awareness is opt-in.
- Existing code that constructs `BackgroundJobServerOptions` does not need to change.

Add a default implementation, for example `JobServerResource`, with these semantics:

- Stores the latest capacity value in a thread-safe way.
- Starts as unavailable until the first successful capacity computation when a reporter is configured.
- `CanAllocate()` returns the latest stored value.
- `CapacityReporter` validates that `computeCapacity` is non-null and `interval` is positive.
- The reporter calls `computeCapacity` once at server startup, then once per interval.

## Server Behavior

`BackgroundJobServer` should pass `BackgroundJobServerOptions.Resource` into the internal server process and worker process configuration.

Worker behavior:

- Before calling `IStorageConnection.FetchNextJob`, a worker checks the configured resource provider.
- If no provider is configured, the worker proceeds exactly as it does today.
- If `CanAllocate()` returns `true`, the worker proceeds to fetch a job.
- If `CanAllocate()` returns `false`, the worker waits briefly using the server stopping token, then checks again.
- The wait must be cancellation-aware so shutdown remains responsive.
- A worker must not fetch or reserve a job while capacity is unavailable.
- Once a job has been fetched, current processing, state transitions, requeue behavior, and finalization behavior remain unchanged.

Capacity reporter behavior:

- Add an internal background process that owns periodic capacity computation.
- The process uses the server lifecycle shutdown token, not the caller token passed to `WaitForShutdownAsync`.
- On startup, compute capacity before workers are allowed to fetch jobs.
- On each successful computation, update the provider's latest value and update server metadata.
- If computation throws, log the exception and retain the previous capacity value.
- If the first computation throws, the server remains unavailable until a later successful computation.

Timer behavior:

- Use `PeriodicTimer` where the target framework supports it.
- For older target frameworks, use an internal compatibility loop based on cancellation-aware delays.
- Do not raise target framework requirements only to use `PeriodicTimer`.

## Storage And Monitoring

Extend existing server metadata without changing the SQL schema:

- Add `CanAllocate` to `Hangfire.Server.ServerContext`.
- Add `CanAllocate` to SQL Server's serialized `ServerData`.
- Add `CanAllocate` to `Hangfire.Storage.Monitoring.ServerDto`.
- Persist the value in the existing serialized `Server.Data` JSON.
- Treat older server records that do not contain the field as available.

SQL Server behavior:

- `AnnounceServer` serializes `CanAllocate` with existing server data.
- Capacity updates reuse the existing server row and update the serialized data.
- `Servers()` deserializes the field and exposes it in `ServerDto`.
- No SQL table or migration change is required.

Dashboard behavior:

- The core feature does not require dashboard UI changes.
- After monitoring exposes `CanAllocate`, the dashboard can optionally display server availability in the servers page.

## .NET Core Integration

The existing `AddHangfireServer` paths should pass through `BackgroundJobServerOptions.Resource` without additional registration requirements.

Typical usage:

```csharp
var resource = new JobServerResource();

resource.CapacityReporter(
    computeCapacity: async () => await localCapacityProbe.CanAcceptMoreJobs(),
    interval: TimeSpan.FromSeconds(5));

services.AddHangfireServer(options =>
{
    options.Resource = resource;
});
```

Applications that do not configure `options.Resource` keep current behavior.

## Failure Modes

- Capacity computation failure:
  - Log the exception.
  - Keep the previous capacity value.
  - If there has not been a successful computation yet, remain unavailable.

- Storage update failure while reporting capacity:
  - Log the exception.
  - Do not stop workers solely because metadata could not be updated.
  - The local `CanAllocate()` value still controls local fetching.

- Server shutdown:
  - The reporter and worker wait loops observe the server shutdown/stopping tokens.
  - Shutdown must not wait for another capacity interval before stopping.

## Test Plan

Core tests:

- `BackgroundJobServerOptions` defaults `Resource` to null.
- A null resource preserves current worker fetching behavior.
- A worker does not call `FetchNextJob` while `CanAllocate()` is false.
- A worker resumes fetching after capacity changes to true.
- A fetched job is still processed and finalized according to existing behavior even if capacity changes during execution.
- The capacity reporter calls `computeCapacity` once on startup and again after the configured interval.
- The capacity reporter stops on the server shutdown token.
- Reporter exceptions are logged and do not crash the server process.
- Before the first successful computation, a configured reporter leaves the server unavailable.

SQL Server tests:

- `AnnounceServer` serializes `CanAllocate`.
- `Servers()` returns `CanAllocate` for new server records.
- `Servers()` treats old server records without `CanAllocate` as available.
- Updating capacity changes the existing server metadata without requiring a schema migration.

Integration checks:

- Build `Hangfire.Core`, `Hangfire.SqlServer`, `Hangfire.NetCore`, and `Hangfire.AspNetCore`.
- Run focused xUnit tests for server options, worker behavior, capacity reporting, server metadata, and SQL Server monitoring.

## Assumptions

- Boolean capacity is sufficient for the first version.
- Hangfire's pull-based worker model remains the dispatch mechanism.
- Resource awareness is local to each server.
- `WorkerCount` remains the maximum local concurrency setting.
- The capacity provider controls whether new jobs are fetched, not whether already fetched jobs continue running.
- No SQL schema migration is required for this version.
