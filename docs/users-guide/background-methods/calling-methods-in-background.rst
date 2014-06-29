Calling methods in background
=============================

Fire-and-forget method invocation has never been simpler. As you can already know from the :doc:`Quick start </quickstart>` guide, you should only pass a lambda expression with the corresponding method and its arguments:

.. code-block:: c#

  BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));

The ``Enqueue`` method does not call the target method immediately, it runs the following steps instead:

1. Serialize a method information and all its arguments.
2. Create a new background job based on the serialized information.
3. Save background job to a persistent storage.
4. Enqueue background job to its queue.

After these steps were performed, the ``BackgroundJob.Enqueue`` method immediately returns to a caller. Another Hangfire component, called :doc:`Hangfire Server <../background-processing/processing-background-jobs>`, checks the persistent storage for enqueued background jobs and performs them in a reliable way. 

Enqueued jobs are being handled by a dedicated pool of worker threads. The following process is being invoked by each worker:

1. Fetch next job and hide it from other workers.
2. Perform a job and all its extension filters.
3. Remove a job from the queue.

So, the job is being removed only after processing succeeded. Even if a process was terminated during the performance, Hangfire will perform a compensation logic to guarantee the processing of each job.

Each storage has its own implementation for each of these steps and compensation logic mechanisms:

* **SQL Server** implementation uses periodical checks to fetch next jobs. If a process was terminated, the job will be re-queued only after ``InvisibilityTimeout`` expiration (30 minutes by default).
* **MSMQ** implementation uses transactional queues, so there is no need for periodic checks. Jobs are being fetched almost immediately after enqueueing.
* **Redis** implementation uses blocking ``BRPOPLPUSH`` command, so jobs are being fetched immediately, as with MSMQ. But in case of process termination, they are being enqueued only after timeout expiration that defaults to 15 minutes.