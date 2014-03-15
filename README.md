HangFire 
=========

[![Build status](https://ci.appveyor.com/api/projects/status/qejwc7kshs1q75m4)](https://ci.appveyor.com/project/odinserj/hangfire)

HangFire helps to perform **background jobs** inside your **ASP.NET application** in a simple, efficient and transparent way. It allows you to define, create and process these jobs asynchronously. With this library, you can move your long running tasks out of the request processing pipeline without losing control over them:

- Mass newsletter
- Batch import from xml, csv, json
- Creation of archives
- Firing off web hooks
- Deleting users
- Building different graphs
- Image processing
- *etc.*

HangFire stores information about jobs in a persistent storage ([SQL Server](http://www.microsoft.com/sql‎) and [Redis](http://redis.io) are supported). So, all of this information is retained after application restart.

Installation
-------------

To install HangFire into your **ASP.NET application** with **SQL Server** storage, type the following command into the Package Manager Console window:

<pre style="background-color: #202020;border: 4px solid silver;border-radius: 3px;color: #E2E2E2;display: block;padding: 10px;">PM> Install-Package HangFire -Pre</pre>

Configuration
--------------

After installing the package, open the `App_Start\HangFireConfig.cs` file in your project and set the connection string for your SQL Server database.

```csharp
Storage.Current = new SqlServerStorage(@"Server=.\sqlexpress; Database=MyDatabase; Trusted_Connection=True;");
```

To test your configuration, build the project and open the following URL in a browser:

<div style="border-radius: 0;border:solid 3px #ccc;background-color:#fcfcfc;box-shadow: 1px 1px 1px #ddd inset, 1px 1px 1px #eee;padding:3px 7px;">
<span style="color: #666;">http://&lt;your-site&gt;</span>/hangfire.axd
</div>

If you see a page like this, then the configuration step is finished. Otherwise, please refer to the [Troubleshooting page](https://github.com/odinserj/HangFire/wiki/Installation) in the documentation.

![HangFire Dashboard](https://github.com/odinserj/hangfire/raw/master/content/dashboard_min.png)

Usage
------

HangFire consist of the four parts. The **Client** creates background jobs and places them into the **Storage**. The **Server** fetches jobs from the Storage and processes them. The **Monitor** provides the ability to see what's going on with your background jobs.

### Add a job…

Job is a method invocation that will be performed asynchronously on the HangFire Server side. To create a job, you need to choose a method that will be called and define arguments of this method.

The first and default job creation method is based on job queue. Queue contains jobs that will be performed in the FIFO order. To enqueue a job, call the following method.

```csharp
BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));
```

You can also tell HangFire to delay the performance of a job:

```csharp
BackgroundJob.Schedule(() => Console.WriteLine("Hello, world!"), TimeSpan.FromDays(1));
```

To learn more about different options of the job creation process, see the [corresponding page](https://github.com/odinserj/HangFire/wiki/Creating-jobs) in the documentation.

### … and relax!

HangFire processes each job in a reliable way. Your can stop your application, kill it using task manager, shutdown your computer<sup>1</sup>, enqueue broken jobs. HangFire contains auto-retrying facilities, or you can retry failed job manually using the integrated Monitor.

So, HangFire is an ideal solution for performing background jobs in ASP.NET applications, because IIS Application Pools contain several mechanisms that can stop your application.

* Idle Time-out
* Recycling, including on configuration changes
* Rapid-Fail protection
* Application re-deployment

<sup>1</sup> Only processing is reliable. If your storage became broken, HangFire can not do anything. To guarantee the processing of each job, you should guarantee the reliability of your storage.

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