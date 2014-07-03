Configuring the degree of parallelism
======================================

Background jobs are being processed by a dedicated pool of worker threads that run inside Hangfire Server subsystem. When you start the background job server, it initializes the pool and starts the fixed amount of workers. You can specify their number by passing the value through the :doc:`OWIN bootstrapper <../getting-started/owin-bootstrapper>`:

.. code-block:: c#

   app.UseHangfire(config =>
   {
       config.UseServer(Environment.ProcessorCount * 5);
   });
   
If you use Hangfire inside a Windows service or console app, just do the following:

.. code-block:: c#

    var options = new BackgroundJobServerOptions
    {
        // This is the default value
        WorkerCount = Environment.ProcessorCount * 5
    };

    var server = new BackgroundJobServer(options);

Worker pool uses dedicated threads to process jobs separatly from requests to let you to process either CPU intensive or I/O intensive tasks as well and configure the degree of parallelism manually.
