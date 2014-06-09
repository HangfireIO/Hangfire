Calling methods with delay
===========================

Sometimes you may want to postpone a method invocation, for example, to send an email to newly registered users after a day since their registration. To do this, just call the ``BackgroundJob.Schedule`` method and pass the needed time span:

.. code-block:: c#

   BackgroundJob.Schedule(
       () => Console.WriteLine("Hello, world"),
       TimeSpan.FromDays(1));

HangFire Server periodically check the schedule to enqueue scheduled jobs to their queues, allowing workers to perform them. By default, check interval is equal to ``15 seconds``, but you can change it, just pass the corresponding option to the ``BackgroundJobServer`` or ``AspNetBackgroundJobServer`` ctor:

.. code-block:: c#

  var options = new BackgroundJobServerOptions
  {
      SchedulePollingInterval = TimeSpan.FromMinutes(1)
  };

  var server = new AspNetBackgroundJobServer(options);
  server.Start();

If you are processing your jobs inside an ASP.NET application, you should be warned about some setting that may prevent your scheduled jobs to be performed in-time. To avoid that behavour, perform the following steps:

* `Disable Idle Timeout <http://bradkingsley.com/iis7-application-pool-idle-time-out-settings/>`_ – set its value to ``0``.
* Use the `application auto-start <http://weblogs.asp.net/scottgu/auto-start-asp-net-applications-vs-2010-and-net-4-0-series>`_ feature.