Overview
=========

Hangfire is a framework for background job processing. It allows you to increase the capacity of your system by offloading task execution outside of the request processing pipeline.

.. image:: hangfire-workflow.png
   :alt: Hangfire Workflow
   :align: center

Client
-------

Hangfire Client allows you to create background jobs and that will be processed by Hangfire Server. 

.. code-block:: c#

   BackgroundJob.Enqueue(() => Console.WriteLine("Hello!"));
   
   var client = new BackgroundJobClient();
   client.Enqueue(() => Console.WriteLine("Hello!"));

Job Storage
------------

All background jobs are being saved to a persistent storage. All storage operations are abstracted, currently Hangfire supports the following storages:

* SQL Server 2008 and later of any edition (including Express)
* SQL Azure
* Redis

SQL Server storage can be empowered with MSMQ or RabbitMQ to lower the processing latency.

Server
-------

Hangfire server asks the storage for pending background jobs and process them.

.. code-block:: c#

   var server = new BackgroundJobServer();
   server.Start();

.. code-block:: c#

   app.UseHangfire(config => config.UseServer());