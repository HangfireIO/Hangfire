Advanced Features
==================

Cancellation tokens
--------------------

HangFire can tell your methods were aborted or canceled due to shutdown event, so you can stop them gracefully using job cancellation tokens that are similar to the regular ``CancellationToken`` class.

.. code-block:: c#

   public void Method(IJobCancellationToken token)
   {
       for (var i = 0; i < Int32.MaxValue; i++)
       {
           token.ThrowIfCancellationRequested();
           Thread.Sleep(1000);
       }
   }

IoC Containers
---------------

In case you want to improve the testability of your job classes or simply don't want to use a huge amount of different factories, you should use instance methods instead of static ones. But you either need to somehow pass the dependencies into these methods and the default job activator does not support parameterful constructors.

Don't worry, you can use your favourite IoC container that will instantiate your classes. There are two packages, ``HangFire.Ninject`` and ``HangFire.Autofac`` for their respective containers. If you are using another container, please, write it yourself (on a basis of the given packages) and contribute to HangFire project.

Logging
--------

HangFire uses the ``Common.Logging`` library to log all its events. It is a generic library and you can plug it to your logging framework using adapters. Please, see the list of available adapters on `NuGet Gallery
<https://www.nuget.org/packages?q=common.logging>`_.

Web Garden and Web Farm friendly
---------------------------------

You can run multiple HangFire instances, either on the same or different machines. It uses distributed locking to prevent race conditions. Each HangFire instance is redundant, and you can add or remove instances seamlessly (but control the queues they listen).

Multiple queues processing
---------------------------

HangFire can process multiple queues. If you want to prioritize your jobs or split the processing across your servers (some processes the ``archive`` queue, others – the ``images`` queue, etc), you can tell HangFire about your decisions.

To place a job into a different queue, use the ``QueueAttribute`` class on your method:

.. code-block:: c#

   [Queue("critical")]
   public void SomeMethod() { }

   BackgroundJob.Enqueue(() => SomeMethod());

To start to process multiple queues, tell the ``BackgroundJobServer`` (or ``AspNetBackgroundJobServer``) class which queues to listen:

.. code-block:: c#

   var options = new BackgroundJobServerOptions
   {
       Queues = new[] { "critical", "default" }
   };
   var server = new AspNetBackgroundJobServer( options );

The order is important, workers will fetch jobs from the ``critical`` queue first, and then from the ``default`` queue.

Concurrency level control
--------------------------

HangFire uses its own fixed worker thread pool to consume queued jobs. Default worker count is set to ``Environment.ProcessorCount * 5``. This number is optimized both for CPU-intensive and I/O intensive tasks. If you experience excessive waits or context switches, you can configure amount of workers manually:

.. code-block:: c#

   var server = new AspNetBackgroundJobServer(100);

Process jobs anywhere
----------------------

By default, the job processing is made within an ASP.NET application. But you can process jobs either in the console application, Windows Service or anywhere else. Everithing you need is to install ``HangFire.Core`` package and use the ``BackgroundJobServer`` instead of the ``AspNetBackgroundJobServer`` class.

Extensibility
--------------

HangFire is build to be as generic as possible. You can extend the following parts:

* storage implementation;
* states subsystem (including the creation of new states);
* job creation process;
* job performance process;
* state changing process;
* job activation process.

Some of core components are made as extensions: ``QueueAttribute``, ``PreserveCultureAttribute``, ``RetryAttribute``, ``SqlServerStorage``, ``RedisStorage``, ``NinjectJobActivator``, ``AutofacJobActivator``, ``ScheduledState``.
