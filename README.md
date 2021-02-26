Hangfire 
=========

[![Official Site](https://img.shields.io/badge/site-hangfire.io-blue.svg)](http://hangfire.io) [![Latest version](https://img.shields.io/nuget/v/Hangfire.svg)](https://www.nuget.org/packages?q=hangfire) [![License LGPLv3](https://img.shields.io/badge/license-LGPLv3-green.svg)](http://www.gnu.org/licenses/lgpl-3.0.html) [![codecov](https://codecov.io/gh/HangfireIO/Hangfire/branch/master/graph/badge.svg)](https://codecov.io/gh/HangfireIO/Hangfire) [![Coverity Scan Build Status](https://scan.coverity.com/projects/4423/badge.svg)](https://scan.coverity.com/projects/hangfireio-hangfire)

## Build Status

&nbsp; | `master` | `dev`
--- | --- | --- 
**Windows** | [![Windows Build Status](https://ci.appveyor.com/api/projects/status/70m632jkycqpnsp9/branch/master?svg=true)](https://ci.appveyor.com/project/odinserj/hangfire-525)  | [![Windows Build Status](https://ci.appveyor.com/api/projects/status/70m632jkycqpnsp9/branch/dev?svg=true)](https://ci.appveyor.com/project/odinserj/hangfire-525) 
**Linux / OS X** | [![Travis CI Build Status](https://travis-ci.org/HangfireIO/Hangfire.svg?branch=master)](https://travis-ci.org/HangfireIO/Hangfire) | [![Linux and OS X Build Status](https://travis-ci.org/HangfireIO/Hangfire.svg?branch=dev)](https://travis-ci.org/HangfireIO/Hangfire)

## Overview

Incredibly easy way to perform **fire-and-forget**, **delayed** and **recurring jobs** inside **ASP.NET applications**. CPU and I/O intensive, long-running and short-running jobs are supported. No Windows Service / Task Scheduler required. Backed by Redis, SQL Server, SQL Azure and MSMQ.

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

Hangfire is a .NET Framework alternative to [Resque](https://github.com/resque/resque), [Sidekiq](http://sidekiq.org), [delayed_job](https://github.com/collectiveidea/delayed_job), [Celery](http://www.celeryproject.org).

![Hangfire Dashboard](http://hangfire.io/img/ui/dashboard-sm.png)

Installation
-------------

Hangfire is available as a NuGet package. You can install it using the NuGet Package Console window:

```
PM> Install-Package Hangfire
```

After installation, update your existing [OWIN Startup](http://www.asp.net/aspnet/overview/owin-and-katana/owin-startup-class-detection) file with the following lines of code. If you do not have this class in your project or don't know what is it, please read the [Quick start](http://docs.hangfire.io/en/latest/quick-start.html) guide to learn about how to install Hangfire.

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

This is an incomplete list of features; to see all of them, check the [official site](http://hangfire.io) and the [documentation](http://docs.hangfire.io).

[**Fire-and-forget tasks**](http://docs.hangfire.io/en/latest/background-methods/calling-methods-in-background.html)

Dedicated worker pool threads execute queued background jobs as soon as possible, shortening your request's processing time.

```csharp
BackgroundJob.Enqueue(() => Console.WriteLine("Simple!"));
```

[**Delayed tasks**](http://docs.hangfire.io/en/latest/background-methods/calling-methods-with-delay.html)

Scheduled background jobs are executed only after a given amount of time.

```csharp
BackgroundJob.Schedule(() => Console.WriteLine("Reliable!"), TimeSpan.FromDays(7));
```

[**Recurring tasks**](http://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html)

Recurring jobs have never been simpler; just call the following method to perform any kind of recurring task using the [CRON expressions](http://en.wikipedia.org/wiki/Cron#CRON_expression).

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

You can process background tasks in any OWIN-compatible application framework, including [ASP.NET MVC](http://www.asp.net/mvc), [ASP.NET Web API](http://www.asp.net/web-api), [FubuMvc](http://fubu-project.org), [Nancy](http://nancyfx.org), etc. Forget about [AppDomain unloads, Web Garden & Web Farm issues](http://haacked.com/archive/2011/10/16/the-dangers-of-implementing-recurring-background-tasks-in-asp-net.aspx/) – Hangfire is reliable for web applications from scratch, even on shared hosting.

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

Questions? Problems?
---------------------

Open-source projects develop more smoothly when discussions are public.

If you have any questions, problems related to Hangfire usage or if you want to discuss new features, please visit the [discussion forum](http://discuss.hangfire.io). You can sign in there using your existing Google or GitHub account, so it's very simple to start using it.

If you've discovered a bug, please report it to the [Hangfire GitHub Issues](https://github.com/HangfireIO/Hangfire/issues?state=open). Detailed reports with stack traces, actual and expected behaviours are welcome.

Related Projects
-----------------

Please see the [Extensions](http://hangfire.io/extensions.html) page on the official site.

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

Copyright © 2013-2021 Sergey Odinokov.

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

Legal
------

By submitting a Pull Request, you disavow any rights or claims to any changes submitted to the Hangfire project and assign the copyright of those changes to Sergey Odinokov.

If you cannot or do not want to reassign those rights (your employment contract for your employer may not allow this), you should not submit a PR. Open an issue and someone else can do the work.

This is a legal way of saying "If you submit a PR to us, that code becomes ours". 99.9% of the time that's what you intend anyways; we hope it doesn't scare you away from contributing.
