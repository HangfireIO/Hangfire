HangFire 
=========

[![Build status](https://ci.appveyor.com/api/projects/status/qejwc7kshs1q75m4)](https://ci.appveyor.com/project/odinserj/hangfire)

HangFire gives you a simple way to kick off **long-running processes** from the **ASP.NET request processing pipeline**. Asynchronous, transparent, reliable, efficient processing. No Windows service/ Task Scheduler required. Even ASP.NET is not required.

Improve the responsiveness of your web application. Do not force your users to wait when the application performs the following tasks:

- mass notifications/newsletter;
- batch import from xml, csv, json;
- creation of archives;
- firing off web hooks;
- deleting users;
- building different graphs;
- image processing;
- *…and so on.*

Just wrap your long-running process to a method and instruct HangFire to create a **background job** based on this method. All backround jobs are being saved to a **persistent storage** ([SQL Server](http://www.microsoft.com/sql‎) or [Redis](http://redis.io)) and performed on a dedicated **worker thread** in a reliable way inside or outside of your ASP.NET application.

HangFire is a .NET Framework alternative to [Resque](https://github.com/resque/resque), [Sidekiq](http://sidekiq.org), [delayed_job](https://github.com/collectiveidea/delayed_job). 

Installation
-------------

HangFire has a [couple of packages](https://www.nuget.org/packages?q=hangfire) available on NuGet. To install HangFire into your **ASP.NET application** with **SQL Server** storage, type the following command into the Package Manager Console window:

<pre style="background-color: #202020;border: 4px solid silver;border-radius: 3px;color: #E2E2E2;display: block;padding: 10px;">PM> Install-Package HangFire -Pre</pre>

During the installation, Package Manager will update your `web.config` file to add the `hangfire.axd` HTTP handler. It also adds the autoload `HangFireConfig` class that contains default configuration and code required to start the job processing.

### Basic configuration

After installing the package, open the `App_Start\HangFireConfig.cs` file in your project and set the connection string for your SQL Server database.

```csharp
/* ... */
public static void Start()
{
    JobStorage.Current = new SqlServerStorage(
        @"Server=.\sqlexpress; Database=MyDatabase; Trusted_Connection=True;");
    /* ... */
```

All HangFire SQL Server objects like tables, indexes, etc. will be installed during the first application start-up. If you want to install them manually, run the `packages\HangFire.SqlServer.*\tools\install.sql` script.

### Upgrading

Please, see the [Release notes](https://github.com/odinserj/HangFire/releases).

Usage
------

Let's start with Monitor to test your configuration. Please, build the project and open the following URL in a browser:

<div style="border-radius: 0;border:solid 3px #ccc;background-color:#fcfcfc;box-shadow: 1px 1px 1px #ddd inset, 1px 1px 1px #eee;padding:3px 7px;">
<span style="color: #666;">http://&lt;your-site&gt;</span>/hangfire.axd
</div>

![HangFire Dashboard](https://github.com/odinserj/hangfire/raw/master/content/dashboard_min.png)

### Add a job…

To create a job, you should specify the **method** that will be called during the performance of a job, its **arguments** and **state**. Method and its arguments tells HangFire *what* needs to performed, and the state tells *how* it should be performed.

#### Enqueued state

This is the main state, it tells HangFire to perform the job, i.e. call the specified method with the given arguments. The job is being added to the specified queue (`"default"` by default). The queue is listened by a couple of dedicated workers, that fetch jobs and perform them.

```csharp
BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));
```

#### Scheduled state

If you want to delay the method invocation for a certain time, use the scheduled state. After the given delay, it will be moved to the Enqueued state and performed as usually.

```csharp
BackgroundJob.Schedule(() => Console.WriteLine("Hello, world!"), TimeSpan.FromDays(1));
```

#### Instance methods

All the examples above used static method invocation. However, you can instruct HangFire to use instance methods as well:

```csharp
public class NotificationService
{
    public void SendNotification(int userId, string template)
    {
        /* ... */
    }
}
/* ... */
BackgroundJob.Enqueue<NotificationService>(x => x.SendNotification(1, "hello"));
```

During the performance of a job, HangFire Worker will activate an instance of the job and then invoke the given method. Job activation is performed by the `JobActivator.Current` instance. Default `JobActivator` uses `Activator.CreateInstance` method, so it can instantiate only classes with default constructor.

### … and relax

HangFire processes each job in a reliable way. Your can stop your application, kill it using task manager, shutdown your computer<sup>1</sup>, enqueue broken jobs. HangFire contains auto-retrying facilities, or you can retry failed job manually using the integrated Monitor.

So, HangFire is an ideal solution for performing background jobs in ASP.NET applications, because IIS Application Pools contain several mechanisms that can stop your application.

* Idle Time-out
* Recycling, including on configuration changes
* Rapid-Fail protection
* Application re-deployment

<sup>1</sup> Only processing is reliable. If your storage became broken, HangFire can not do anything. To guarantee the processing of each job, you should guarantee the reliability of your storage.

Advanced usage
---------------

### IoC Containers

However, if you use an IoC container, you can create your own job activator and register it.

```csharp
public class AutofacActivator : JobActivator
{
    private readonly ILifetimeScope _scope;

    public AutofacActivator(ILifetimeScope scope)
    {
        _scope = scope;
    }

    public object ActivateJob(Type type)
    {
        return _scope.Resolve(type);
    }
}

/* Global.asax.cs */
JobActivator.Current = new AutofacActivator(scope);
```

After that, you could use any constructors.

```csharp
public class NotificationService : IDisposable
{
    private readonly DbContext _context;
    private readonly IEmailService _email;    

    public NotificationService(DbContext context, IEmailService email)
    {
        _context = context;
        _email = email;
    }

    public void SendNotification(int userId, string template)
    {
        var user = _context.Users.Get(1);
        _email.Send(user.Email, template);
    }

    // Instances of classes that implement the IDisposable
    // interface, will be disposed.
    public void Dispose()
    {
        _context.Dispose();
    }
}
```

Questions? Problems? 
---------------------

If you have installation issues, problems or questions, just let me know. Open a [new issue](https://github.com/odinserj/HangFire/issues?state=open) and I'll answer to it.

License
--------

Copyright © 2013-2014 Sergey Odinokov.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see [http://www.gnu.org/licenses/](http://www.gnu.org/licenses).