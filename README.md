Hangfire 
=========

[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](https://www.hangfire.io) [![Latest version](https://img.shields.io/nuget/v/Hangfire.svg?label=release)](https://www.nuget.org/packages?q=hangfire) [![Downloads](https://img.shields.io/nuget/dt/Hangfire.Core.svg)](https://www.nuget.org/packages/Hangfire.Core/) [![License LGPLv3](https://img.shields.io/badge/license-LGPLv3-green.svg)](https://www.gnu.org/licenses/lgpl-3.0.html) [![Coverity Scan](https://scan.coverity.com/projects/4423/badge.svg?flat=1)](https://scan.coverity.com/projects/hangfireio-hangfire)

## Build Status

&nbsp; | `main` | `dev`
--- | --- | --- 
**AppVeyor** | [![Windows Build Status](https://ci.appveyor.com/api/projects/status/70m632jkycqpnsp9/branch/main?svg=true)](https://ci.appveyor.com/project/HangfireIO/hangfire-525)  | [![Windows Build Status](https://ci.appveyor.com/api/projects/status/70m632jkycqpnsp9/branch/dev?svg=true)](https://ci.appveyor.com/project/HangfireIO/hangfire-525) 

## Overview

Incredibly easy way to perform **fire-and-forget**, **delayed** and **recurring jobs** in **.NET applications**. CPU and I/O intensive, long-running and short-running jobs are supported. No Windows Service / Task Scheduler required. Backed by Redis, SQL Server, SQL Azure and MSMQ.

Hangfire provides a unified programming model to handle background tasks in a **reliable way** and run them on shared hosting, dedicated hosting or in cloud. You can start with a simple setup and grow computational power for background jobs with time for these scenarios:

- mass notifications/newsletters
- batch import from xml, csv or json
- creation of archives
- firing off web hooks
- deleting users
- building different graphs
- image/video processing
- purging temporary files
- recurring automated reports
- database maintenance
- *…and so on*

Hangfire is a .NET alternative to [Resque](https://github.com/resque/resque), [Sidekiq](https://sidekiq.org), [delayed_job](https://github.com/collectiveidea/delayed_job), [Celery](https://www.celeryproject.org).

![Hangfire Dashboard](https://www.hangfire.io/img/ui/dashboard-sm.png)

Installation
-------------

Hangfire is available as a NuGet package. You can install it using the NuGet Package Console window:

```
PM> Install-Package Hangfire
```

After installation, update your existing [OWIN Startup](https://www.asp.net/aspnet/overview/owin-and-katana/owin-startup-class-detection) file with the following lines of code. If you do not have this class in your project or don't know what is it, please read the [Quick start](https://docs.hangfire.io/en/latest/getting-started/index.html) guide to learn about how to install Hangfire.

```csharp
public void Configuration(IAppBuilder app)
{
    GlobalConfiguration.Configuration.UseSqlServerStorage("<connection string or its name>");
    
    app.UseHangfireServer();
    app.UseHangfireDashboard();
}
```

Usage
------

This is an incomplete list of features; to see all of them, check the [official site](https://www.hangfire.io) and the [documentation](https://docs.hangfire.io).

[**Fire-and-forget tasks**](https://docs.hangfire.io/en/latest/background-methods/calling-methods-in-background.html)

Dedicated worker pool threads execute queued background jobs as soon as possible, shortening your request's processing time.

```csharp
BackgroundJob.Enqueue(() => Console.WriteLine("Simple!"));
```

[**Delayed tasks**](https://docs.hangfire.io/en/latest/background-methods/calling-methods-with-delay.html)

Scheduled background jobs are executed only after a given amount of time.

```csharp
BackgroundJob.Schedule(() => Console.WriteLine("Reliable!"), TimeSpan.FromDays(7));
```

[**Recurring tasks**](https://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html)

Recurring jobs have never been simpler; just call the following method to perform any kind of recurring task using the [CRON expressions](https://en.wikipedia.org/wiki/Cron#CRON_expression).

```csharp
RecurringJob.AddOrUpdate(() => Console.WriteLine("Transparent!"), Cron.Daily);
```

**Continuations**

Continuations allow you to define complex workflows by chaining multiple background jobs together.

```csharp
var id = BackgroundJob.Enqueue(() => Console.WriteLine("Hello, "));
BackgroundJob.ContinueWith(id, () => Console.WriteLine("world!"));
```

**Process background tasks inside a web application…**

You can process background tasks in any OWIN-compatible application framework, including [ASP.NET MVC](https://www.asp.net/mvc), [ASP.NET Web API](https://www.asp.net/web-api), [FubuMvc](https://fubu-project.org), [Nancy](https://nancyfx.org), etc. Forget about [AppDomain unloads, Web Garden & Web Farm issues](https://haacked.com/archive/2011/10/16/the-dangers-of-implementing-recurring-background-tasks-in-asp-net.aspx/) – Hangfire is reliable for web applications from scratch, even on shared hosting.

```csharp
app.UseHangfireServer();
```

**… or anywhere else**

In console applications, Windows Service, Azure Worker Role, etc.

```csharp
using (new BackgroundJobServer())
{
    Console.WriteLine("Hangfire Server started. Press ENTER to exit...");
    Console.ReadLine();
}
```

**Resource-aware background servers and operational controls**

Background servers can be made aware of local capacity. When a server is resource constrained or intentionally draining, it stays alive, keeps heartbeating, remains visible in monitoring and lets already fetched jobs finish normally, but workers stop fetching new jobs until capacity returns.

Resource awareness is opt-in and backward compatible. Applications that do not configure `BackgroundJobServerOptions.Resource` keep the existing Hangfire behavior.

The feature includes:

- `IJobServerResource` and `JobServerResource` for reporting whether a server can allocate more work.
- Rich resource snapshots with allocation state, reason, timestamps, drain state and capacity-check failure counters.
- Graceful server drain mode for deployments, node maintenance and operational pauses.
- Per-queue resource and drain policies, so a server can pause one risky queue while continuing to process safe queues.
- Dashboard drain and resume commands from the Servers page, protected by dedicated resource-command authorization.
- Resource history events and queue availability summaries exposed through monitoring APIs.
- Built-in provider helpers for memory limits, disk free-space checks, CPU-load checks and composite resource checks.
- Worker-side checks before `FetchNextJob`, preventing constrained servers from reserving new jobs.
- ASP.NET Core hosted-service integration that can enter drain mode when the host starts shutting down.
- SQL Server storage support for operational resource metadata using existing storage primitives.

Use `JobServerResource` when capacity can be computed periodically:

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

For self-hosted servers, pass the resource through `BackgroundJobServerOptions`:

```csharp
var resource = new JobServerResource();

resource.CapacityReporter(
    computeCapacity: () => Task.FromResult(Environment.WorkingSet < 512 * 1024 * 1024),
    interval: TimeSpan.FromSeconds(10));

var options = new BackgroundJobServerOptions
{
    Resource = resource
};

using (new BackgroundJobServer(options))
{
    Console.WriteLine("Hangfire Server started. Press ENTER to exit...");
    Console.ReadLine();
}
```

Use local drain mode to stop fetching new jobs while letting in-flight jobs finish:

```csharp
resource.Drain("Deployment in progress");

// Later, when the node can accept work again:
resource.Resume();
```

Pause or drain individual queues when only part of the local workload is constrained:

```csharp
resource.SetQueueState("image-processing", canAllocate: false, reason: "CPU pressure");
resource.SetQueueState("emails", canAllocate: true);

resource.DrainQueue("video-transcoding", "GPU maintenance");
resource.ResumeQueue("video-transcoding");
```

Allow authorized dashboard users to request drain and resume from the Servers page:

```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    ResourceCommandAuthorization = context =>
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true &&
               user.IsInRole("Hangfire Operators");
    }
});
```

For Kubernetes or other orchestrators, enter drain mode on host shutdown before the server stops:

```csharp
services.AddHangfireServer(options =>
{
    options.Resource = resource;
    options.DrainOnApplicationStopping = true;
    options.DrainOnApplicationStoppingReason = "Host is stopping";
    options.ShutdownTimeout = TimeSpan.FromMinutes(2);
});
```

Use built-in resource probes directly or compose several checks:

```csharp
var resource = JobServerResource.FromComposite(
    TimeSpan.FromSeconds(5),
    JobServerResource.FromMemoryLimit(512 * 1024 * 1024, TimeSpan.FromSeconds(5)),
    JobServerResource.FromDiskFreeSpace("C:\\", 5L * 1024 * 1024 * 1024, TimeSpan.FromSeconds(30)),
    JobServerResource.FromCpuLoad(maxCpuLoad: 0.85, interval: TimeSpan.FromSeconds(5)));
```

Custom providers remain simple: implement `IJobServerResource` when all you need is a boolean allocation gate.

```csharp
public sealed class MaintenanceWindowResource : IJobServerResource
{
    public bool CanAllocate()
    {
        return !maintenanceWindow.IsActive;
    }

    public void CapacityReporter(Func<Task<bool>> computeCapacity, TimeSpan interval)
    {
        throw new NotSupportedException("This resource is checked directly.");
    }
}
```

Monitoring code can inspect server state, resource history and queue availability through the monitoring API:

```csharp
var monitor = JobStorage.Current.GetMonitoringApi();

foreach (var server in monitor.Servers())
{
    Console.WriteLine(
        $"{server.Name}: {server.AllocationState}, can allocate = {server.CanAllocate}, reason = {server.AllocationReason}");
}

foreach (var queue in monitor.QueueAvailability())
{
    Console.WriteLine(
        $"{queue.Queue}: {queue.AvailableServers} available, {queue.ConstrainedServers} constrained");
}

foreach (var resourceEvent in monitor.ResourceEvents(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow))
{
    Console.WriteLine(
        $"{resourceEvent.CreatedAt:o} {resourceEvent.ServerId} {resourceEvent.EventType}: {resourceEvent.Reason}");
}
```

Lower-level integrations can also issue commands through storage:

```csharp
using (var connection = JobStorage.Current.GetConnection())
{
    ((JobStorageConnection)connection).SaveServerResourceCommand(
        "worker-01:12345:abcdef",
        new ServerResourceCommand
        {
            Command = "drain",
            Reason = "Manual maintenance",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "ops@example.com"
        });
}
```

Queue-specific operational commands use the same storage command path:

```csharp
using (var connection = JobStorage.Current.GetConnection())
{
    ((JobStorageConnection)connection).SaveServerResourceCommand(
        "worker-01:12345:abcdef",
        new ServerResourceCommand
        {
            Command = "drain",
            Queue = "video-transcoding",
            Reason = "GPU maintenance",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "ops@example.com"
        });
}
```

**Priority-aware and multitenant queues**

Hangfire queues can now be declared with explicit priorities, and SQL Server storage can keep queue identity separate per tenant. This is opt-in: when no `TenantId` is configured, jobs continue to use the existing global queue namespace.

The feature includes:

- `BackgroundJobServerOptions.Queues` as a priority-aware `QueuePriorityCollection`.
- Deterministic SQL Server dequeue ordering by queue priority, then `JobQueue.Id`.
- Optional `BackgroundJobServerOptions.TenantId` for one-tenant-per-server workers.
- `HangfireTenantContext` for flowing tenant id through async job creation.
- Tenant-aware `EnqueuedState.TenantId` for explicit job creation.
- SQL Server `JobQueue.TenantId` storage, where `NULL` means the legacy global queue namespace.
- Server and queue-availability monitoring fields that include tenant scope.
- Storage feature flags for tenant-aware enqueue, fetch, monitoring and dashboard support.
- SQL Server schema version 10 adds the nullable `JobQueue.TenantId` column and the tenant-aware queue index.

Declare queue priorities for a global server:

```csharp
services.AddHangfireServer(options =>
{
    options.Queues = new QueuePriorityCollection
    {
        ["critical"] = 1,
        ["default"] = 2,
        ["bulk"] = 3
    };
});
```

Run one server for tenant `xyz` and the same logical queues:

```csharp
services.AddHangfireServer(options =>
{
    options.TenantId = "xyz";
    options.Queues = new QueuePriorityCollection
    {
        ["emails"] = 1,
        ["reports"] = 2
    };
});
```

Run another server for tenant `abc` with the same queue names, without sharing jobs with `xyz`:

```csharp
services.AddHangfireServer(options =>
{
    options.TenantId = "abc";
    options.Queues = new QueuePriorityCollection
    {
        ["emails"] = 1,
        ["reports"] = 2
    };
});
```

Enqueue jobs for the current tenant with a scoped tenant context:

```csharp
using (HangfireTenantContext.Use("xyz"))
{
    BackgroundJob.Enqueue(() => SendWelcomeEmail(userId));
}
```

Create a tenant-scoped job explicitly when infrastructure code already knows the tenant:

```csharp
var client = new BackgroundJobClient();

client.Create(
    Job.FromExpression(() => GenerateTenantReport("xyz")),
    new EnqueuedState("reports")
    {
        TenantId = "xyz"
    });
```

Global jobs keep working as before and are fetched only by global servers:

```csharp
BackgroundJob.Enqueue(() => RebuildSearchIndex());

services.AddHangfireServer(options =>
{
    options.Queues = new QueuePriorityCollection
    {
        ["default"] = 1
    };
});
```

The same queue name can safely exist in both global and tenant scopes:

```csharp
BackgroundJob.Enqueue(() => SendGlobalDigest());

using (HangfireTenantContext.Use("xyz"))
{
    BackgroundJob.Enqueue(() => SendTenantDigest("xyz"));
}
```

Inspect tenant-aware server and queue availability data from monitoring:

```csharp
var monitor = JobStorage.Current.GetMonitoringApi();

foreach (var server in monitor.Servers())
{
    Console.WriteLine($"{server.Name}: tenant = {server.TenantId ?? "global"}");
}

foreach (var queue in monitor.QueueAvailability())
{
    Console.WriteLine(
        $"{queue.TenantId ?? "global"} / {queue.Queue}: {queue.AvailableServers} available");
}
```

Questions? Problems?
---------------------

Open-source projects develop more smoothly when discussions are public.

If you have any questions, problems related to Hangfire usage or if you want to discuss new features, please visit the [discussion forum](https://discuss.hangfire.io). You can sign in there using your existing Google or GitHub account, so it's very simple to start using it.

If you've discovered a bug, please report it to the [Hangfire GitHub Issues](https://github.com/HangfireIO/Hangfire/issues?state=open). Detailed reports with stack traces, actual and expected behaviours are welcome.

Related Projects
-----------------

Please see the [Extensions](https://www.hangfire.io/extensions.html) page on the official site.

Building the sources
---------------------

Prerequisites:
* [Razor Generator](https://marketplace.visualstudio.com/items?itemName=DavidEbbo.RazorGenerator): Required if you intend to edit the cshtml files.
* Install the MSMQ service (Microsoft Message Queue Server), if not already installed.

Then, create an environment variable with Variable name `Hangfire_SqlServer_ConnectionStringTemplate` and put your connection string in the Variable value field. Example:

* Variable name: `Hangfire_SqlServer_ConnectionStringTemplate`
* Variable value: `Data Source=.\sqlexpress;Initial Catalog=Hangfire.SqlServer.Tests;Integrated Security=True;`

To build a solution and get assembly files, just run the following command. All build artifacts, including `*.pdb` files, will be placed into the `build` folder. **Before proposing a pull request, please use this command to ensure everything is ok.** Btw, you can execute this command from the Package Manager Console window.

```
build
```

To build NuGet packages as well as an archive file, use the `pack` command as shown below. You can find the result files in the `build` folder.

```
build pack
```

To see the full list of available commands, pass the `-docs` switch:

```
build -docs
```

Hangfire uses [psake](https://github.com/psake/psake) build automation tool. All psake tasks and functions defined in `psake-build.ps1` (for this project) and `psake-common.ps1` (for other Hangfire projects) files. Thanks to the psake project, they are very simple to use and modify!

Razor templates are compiled upon save with the [Razor Generator Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=DavidEbbo.RazorGenerator).  You will need this installed if you want to modify the Dashboard UI.

Reporting security issues 
--------------------------

In order to give the community time to respond and upgrade we strongly urge you report all security issues privately. Please email us at [security@hangfire.io](mailto:security@hangfire.io) with details and we will respond ASAP. Security issues always take precedence over bug fixes and feature work. We can and do mark releases as "urgent" if they contain serious security fixes. 

License
--------

Copyright © 2013-2026 Hangfire OÜ.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see [https://www.gnu.org/licenses/](https://www.gnu.org/licenses).

Legal
------

By submitting a Pull Request, you disavow any rights or claims to any changes submitted to the Hangfire project and assign the copyright of those changes to Hangfire OÜ.

If you cannot or do not want to reassign those rights (your employment contract for your employer may not allow this), you should not submit a PR. Open an issue and someone else can do the work.

This is a legal way of saying "If you submit a PR to us, that code becomes ours". 99.9% of the time that's what you intend anyways; we hope it doesn't scare you away from contributing.
