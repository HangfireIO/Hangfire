HangFire <a href="https://ci.appveyor.com/project/odinserj/hangfire"><img title="Build status" width="113" src="https://ci.appveyor.com/api/projects/status/qejwc7kshs1q75m4/branch/master?retina=true" /></a>
=========

#### [Official Site](http://hangfire.io) | [Blog](http://odinserj.net) | [Documentation](http://docs.hangfire.io) | [Forum](http://discuss.hangfire.io) | [Twitter](https://twitter.com/hangfire_net) | [NuGet Packages](https://www.nuget.org/packages?q=hangfire)

Incredibly easy way to perform **fire-and-forget**, **delayed** and **recurring jobs** inside **ASP.NET applications**. No Windows Service / Task Scheduler required. Backed by Redis, SQL Server, SQL Azure or MSMQ.

HangFire provides unified programming model to handle background tasks in a **reliable way** and run them on shared hosting, dedicated hosting or in cloud. You can start with a simple setup and grow computational power for background jobs with time for these scenarios:

- mass notifications/newsletter;
- batch import from xml, csv, json;
- creation of archives;
- firing off web hooks;
- deleting users;
- building different graphs;
- image processing;
- system maintenance;
- *…and so on.*

HangFire is a .NET Framework alternative to [Resque](https://github.com/resque/resque), [Sidekiq](http://sidekiq.org), [delayed_job](https://github.com/collectiveidea/delayed_job).

![HangFire Dashboard](http://hangfire.io/img/succeeded-job.png)

Installation
-------------

See the [Quick start](http://docs.hangfire.io/en/latest/quickstart.html) guide to learn how to install and use HangFire for the first time.

HangFire is available as a NuGet package. So, install it using the NuGet Package Console window:

```
PM> Install-Package HangFire
```

After installing, open the `~/App_Start/HangFireConfig.cs` file and modify the connection string:

```csharp
JobStorage.Current = new SqlServerStorage(
    @"Server=.\sqlexpress; Database=MyDatabase; Trusted_Connection=True;");
```

Usage
------

**Fire-and-forget tasks**

```csharp
BackgroundJob.Enqueue(() => Console.WriteLine("Simple!"));
```

**Delayed tasks**

```csharp
BackgroundJob.Schedule(() => Console.WriteLine("Reliable!"), TimeSpan.FromDays(7));
```

**Recurring tasks**

```csharp
RecurringJob.AddOrUpdate(() => Console.WriteLine("Transparent!"), Cron.Daily);
```

**Run them inside ASP.NET app…**

Forget about AppDomain unloads, Web Garden & Web Farm issues – HangFire is reliable for ASP.NET from scratch, even on Shared Hosting!

```csharp
var server = new AspNetBackgroundJobServer();
server.Start();
```

**… or anywhere else**

In console application, Windows Service, Azure Worker Role, etc.

```csharp
var server = new BackgroundJobServer();
server.Start();
```

This is incomplete list of features, to see all of them, check the [official site](http://hangfire.io) and the [documentation](http://docs.hangfire.io).

Questions? Problems?
---------------------

Open-source project are developing more smoothly, when all discussions are held in public.

If you have any questions, problems related to the HangFire usage or want to discuss new features, please visit the [discussion forum](http://discuss.hangfire.io). You can sign in there using your existing Google or GitHub account, so it's very simple to start using it.

If you've discovered a bug, please report it to the [HangFire GitHub Issues](https://github.com/odinserj/HangFire/issues?state=open). Detailed reports with stack traces, actual and expected behavours are welcome. 

Related Projects
-----------------

* [HangFire.Autofac](https://github.com/odinserj/HangFire.Autofac)
* [HangFire.Ninject](https://github.com/odinserj/HangFire.Ninject)
* [HangFire.SimpleInjector](https://github.com/devmondo/HangFire.SimpleInjector) by [@devmondo](https://github.com/devmondo)
* [HangFire.Windsor](https://github.com/BredStik/HangFire.Windsor) by [@BredStik](https://github.com/BredStik)
* [HangFire.Azure.QueueStorage](https://github.com/odinserj/HangFire.Azure.QueueStorage)
* [HangFire.Azure.ServiceBusQueue](https://github.com/odinserj/HangFire.Azure.ServiceBusQueue)

Roadmap
--------

* Full documentation for product and its API.
* More tutorials and articles that describe the features and use cases.
* ~~Recurring jobs support to fully cover all background needs.~~
* Support for other job storages, ~~including Microsoft Azure Storage~~.
* Make it easier to maintain jobs, even on large-scale systems.
* Deliver the solution to the 90% of ASP.NET developers :smile:.

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

[![githalytics.com alpha](https://cruel-carlota.pagodabox.com/dd58c8cf730a3ed3675202135bb06025 "githalytics.com")](http://githalytics.com/odinserj/HangFire)
