Using SQL Server with MSMQ
===========================

`HangFire.SqlServer.MSMQ <https://www.nuget.org/packages/HangFire.SqlServer.MSMQ/>`_ extension changed the way HangFire handles job queues. Default :doc:`implementation <using-sql-server>` uses regular SQL Server tables to organize queues, and this extensions uses transactional MSMQ queues to process jobs in a more efficient way:

================================ ================= =================
Feature                          Raw SQL Server    SQL Server + MSMQ
================================ ================= =================
Retry after process termination  Timeout           Immediate after
                                 (30 minutes by    restart
                                 default)
Worst job fetch time             Polling Interval  Immediate
                                 (15 seconds by
                                 default)
================================ ================= =================

So, if you want to lower background job processing latency with SQL Server storage, consider switching to using MSMQ.

Installation
-------------

MSMQ support for SQL Server job storage implementation, like other HangFire extensions, is a NuGet package. So, you can install it using NuGet Package Manager Console window:

.. code-block:: powershell

   PM> Install-Package HangFire.SqlServer.MSMQ

Configuration
--------------

To use MSMQ queues, you should do the following steps:

1. Create them manually on each host. Don't forget to grant appropriate permissions.
2. Register all MSMQ queues in current ``SqlServerStorage`` instance.

.. code-block:: c#

   var storage = new SqlServerStorage("<name or connection string>");
   storage.UseMsmqQueues(@".\hangfire-{0}", "critical", "default");
   // or storage.UseMsmqQueues(@".\hangfire-{0}") if you are using only "default" queue.

   JobStorage.Current = storage;

Limitations
------------

* Only transactional MSMQ queues supported for reability reasons inside ASP.NET.
* You can not use both SQL Server Job Queue and MSMQ Job Queue implementations in the same server (see below). This limitation relates to HangFire.Server only. You can still enqueue jobs to whatever queues and watch them both in HangFire.Monitor.

The following case will not work: the ``critical`` queue uses MSMQ, and the ``default`` queue uses SQL Server to store job queue. In this case job fetcher can not make the right decision.

.. code-block:: c#

   var storage = new SqlServerStorage("<connection string>");
   storage.UseMsmqQueues(@".\hangfire-{0}", "critical");

   JobStorage.Current = storage;

   var options = new BackgroundJobServerOptions
   {
       Queues = new [] { "critical", "default" }
   };

   var server = new AspNetBackgroundJobServer(options);
   server.Start();

Transition to MSMQ queues
--------------------------

If you have a fresh installation, just use the ``UseMsmqQueues`` method. Otherwise, your system may contain unprocessed jobs in SQL Server. Since one HangFire.Server instance can not process job from different queues, you should deploy :doc:`multiple instances <../background-processing/running-multiple-server-instances>` of HangFire.Server, one listens only MSMQ queues, another – only SQL Server queues. When the latter finish its work (you can see this from HangFire.Monitor – your SQL Server queues will be removed), you can remove it safely.

If you are using default queue only, do this:

.. code-block:: c#

    /* This server will process only SQL Server table queues, i.e. old jobs */

    var oldStorage = new SqlServerStorage("<connection string>");
    var oldOptions = new BackgroundJobServerOptions
    {
        ServerName = "OldQueueServer" // Pass this to differentiate this server from the next one
    };

    var oldQueueServer = new AspNetBackgroundJobServer(oldOptions, oldStorage);
    oldQueueServer.Start();

    /* This server will process only MSMQ queues, i.e. new jobs */

    // Assign the storage globally, for client, server and monitor.
    JobStorage.Current = 
        new SqlServerStorage("<connection string>").UseMsmqQueues(@".\hangfire-{0}");

    var server = new AspNetBackgroundJobServer();
    server.Start();

If you use multiple queues, do this:

.. code-block:: c#

    /* This server will process only SQL Server table queues, i.e. old jobs */

    var oldStorage = new SqlServerStorage("<connection string>");
    var oldOptions = new BackgroundJobServerOptions
    {
        Queues = new [] { "critical", "default" }, // Include this line only if you have multiple queues
        ServerName = "OldQueueServer" // Pass this to differentiate this server from the next one
    };

    var oldQueueServer = new AspNetBackgroundJobServer(oldOptions, oldStorage);
    oldQueueServer.Start();

    /* This server will process only MSMQ queues, i.e. new jobs */

    // Assign the storage globally, for client, server and monitor.
    JobStorage.Current = 
        new SqlServerStorage("<connection string>").UseMsmqQueues(@".\hangfire-{0}");

    var options = new BackgroundJobServerOptions
    {
        Queues = new [] { "critical", "default" }
    };

    var server = new AspNetBackgroundJobServer(options);
    server.Start();