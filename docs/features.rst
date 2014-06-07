Features
=========

Queue-based processing
-----------------------

Instead of invoking a method synchronously, place it on a persistent queue, and HangFire worker thread will take it and perform within its own execution context:

.. code-block:: c#

   BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));

This method creates a job in the storage and immediately returns control to the caller. HangFire guarantees that the specified method will be called even after the abnormal termination of the host process.

Delayed method invocation
--------------------------

Instead of invoking a method right now, you can postpone its execution for a specified time:

.. code-block:: c#

   BackgroundJob.Schedule(() => Console.WriteLine("Hello, world!"), TimeSpan.FromMinutes(5));

This call also saves a job, but instead of placing it to a queue, it adds the job to a persistent schedule. When the given time elapsed, the job will be added to its queue. Meanwhile you can restart your application – it will be executed anyway.

Recurring tasks
----------------

Recurring job processing was never been easier. All you need is to call a single line of code:

.. code-block:: c#

   RecurringJob.AddOrUpdate(() => Console.Write("Easy!"), Cron.Daily);

HangFire uses `NCrontab <https://code.google.com/p/ncrontab/>`_ library to perform scheduling tasks, so you can use more complex `CRON expressions <http://en.wikipedia.org/wiki/Cron#CRON_expression>`_:

.. code-block:: c#

   RecurringJob.AddOrUpdate(
       () => Console.Write("Powerful!"), 
       "0 12 * */2");

Integrated web interface
-------------------------

Web interface will help you to track the execution of your jobs. See their processing state, watch the statistics. Look at screenshots on http://hangfire.io, and you'll love it.

SQL Server and Redis support
-----------------------------

HangFire uses persistent storage to store jobs, queues and statistics and let them survive application restarts. The storage subsystem is abstracted enough to support both classic SQL Server and fast Redis.

* **SQL Server** provides simplified installation together with usual maintenance plans.
* **Redis** provides awesome speed, especially comparing to SQL Server, but requires additional knowledge.

Automatic retries
------------------

If your method encounters a transient exception, don't worry – it will be retried automatically in a few seconds. If all retry attempts are exhausted, you are able to restart it manually from integrated web interface.

You can also control the retry behavior with the ``RetryAttribute`` class. Just apply it to your method to tell HangFire the number of retry attempts:

.. code-block:: c#

   [Retry(100)]
   public static void GenerateStatistics() { }

   BackgroundJob.Enqueue(() => GenerateStatistics());

Guaranteed processing
----------------------

HangFire was made with the knowledge that the hosting environment can kill all the threads on each line. So, it does not remove the job until it was successfully completed and contains different implicit retry logic to do the job when its processing was aborted.

Instance method calls
----------------------

All the examples above uses static method invocation, but instance methods are supported as well:

.. code-block:: c#

   public class EmailService
   {
       public void Send() { }
   }

   BackgroundJob.Enqueue<EmailService>(x => x.Send());

When a worker sees that the given method is an instance-method, it will activate its class first. By default, the ``Activator.CreateInstace`` method is being used, so only classes with default constructors are supported by default. But you can plug in your IoC container and pass the dependencies through the constructor.

Culture capturing
------------------

When you marshal your method invocation into another execution context, you should be able to preserve some environment settings. Some of them – ``Thread.CurrentCulture`` and ``Thread.CurrentUICulture`` are automatically being captured for you.

It is done by the ``PreserveCultureAttribute`` class that is applied to all of your methods by default.