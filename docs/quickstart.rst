Quick start
============

Installation
-------------

HangFire has a `couple of packages
<https://www.nuget.org/packages?q=hangfire>`_ available on NuGet. To install HangFire into your **ASP.NET application** with **SQL Server** storage, type the following command into the Package Manager Console window:

.. code-block:: powershell

   PM> Install-Package HangFire

During the installation, Package Manager will update your ``web.config`` file to add the `hangfire.axd` HTTP handler. It also adds the autoload ``HangFireConfig`` class that contains default configuration and code required to start the job processing.

Configuration
--------------------

After installing the package, open the ``App_Start\HangFireConfig.cs`` file in your project and set the connection string for your SQL Server database.

.. code-block:: c#

   /* ... */
   public static void Start()
   {
       JobStorage.Current = new SqlServerStorage(
           @"Server=.\sqlexpress; Database=MyDatabase; Trusted_Connection=True;");
       /* ... */

All HangFire SQL Server objects like tables, indexes, etc. will be installed during the first application start-up. If you want to install them manually, run the ``packages\HangFire.SqlServer.*\tools\install.sql`` script.

Let's start with Monitor to test your configuration. Please, build the project and open the following URL in a browser:

.. raw:: html

   <div style="border-radius: 0;border:solid 3px #ccc;background-color:#fcfcfc;box-shadow: 1px 1px 1px #ddd inset, 1px 1px 1px #eee;padding:3px 7px;margin-bottom: 10px;">
       <span style="color: #666;">http://&lt;your-site&gt;</span>/hangfire.axd
   </div>


.. image:: http://hangfire.io/img/succeeded-jobs-sm.png

Usage
------

Add a job…
~~~~~~~~~~~

To create a job, you should specify the **method** that will be called during the performance of a job, its **arguments** and **state**. Method and its arguments tells HangFire *what* needs to performed, and the state tells *how* it should be performed.

Enqueued state
^^^^^^^^^^^^^^^

This is the main state, it tells HangFire to perform the job, i.e. call the specified method with the given arguments. The job is being added to the specified queue (`"default"` by default). The queue is listened by a couple of dedicated workers, that fetch jobs and perform them.

.. code-block:: c#

   BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));

Scheduled state
^^^^^^^^^^^^^^^^

If you want to delay the method invocation for a certain time, use the scheduled state. After the given delay, it will be moved to the Enqueued state and performed as usually.

.. code-block:: c#

   BackgroundJob.Schedule(() => Console.WriteLine("Hello, world!"), TimeSpan.FromDays(1));

… and relax
~~~~~~~~~~~~

HangFire saves your jobs into persistent storage and processes them in a reliable way. It means that you can abort HangFire worker threads, unload application domain or even terminate its process, and your jobs will be processed anyway [#note]_. HangFire flags your job as completed only when the last line of your code was performed, and knows that the job can fail before this last line. It contains different auto-retrying facilities, that can handle either storage errors or errors inside your code.

This is very important for generic hosting environment, such as IIS Server. They can contain different `optimizations, timeouts and error-handling code
<https://github.com/odinserj/HangFire/wiki/IIS-Can-Kill-Your-Threads>`_ (that may cause process termination) to prevent bad things to happen. If you are not using the reliable processing and auto-retrying, your job can be lost. And your end user may wait for its email, report, notification, etc. indefinitely.

.. [#] But when your storage becomes broken, HangFire can not do anything. Please, use different failover strategies for your storage to guarantee the processing of each job in case of a disaster.