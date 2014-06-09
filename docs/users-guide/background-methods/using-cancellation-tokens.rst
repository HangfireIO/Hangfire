Using cancellation tokens
===========================

HangFire provides support for cancellation tokens for your jobs to let them know when a shutdown request was initiated, or job performance was aborted. In the former case the job will be automatically put back to the beginning of its queue, allowing HangFire to process it after restart.

Cancellation tokens are exposed through the ``IJobCancellationToken`` interface. It contains the ``ThrowIfCancellationRequested`` method that throws the ``OperationCanceledException`` when cancellation was requested:

.. code-block:: c#

   public void LongRunningMethod(IJobCancellationToken cancellationToken)
   {
       for (var i = 0; i < Int32.MaxValue; i++)
       {
           cancellationToken.ThrowIfCancellationRequested();

           Thread.Sleep(TimeSpan.FromSeconds(1));
       }
   }

When you want to enqueue such method call as a background job, you can pass the ``null`` value as an argument for the token parameter, or use the ``JobCancellationToken.Null`` property to tell code readers that you are doing things right:

.. code-block:: c#

   BackgroundJob.Enqueue(() => LongRunningMethod(JobCancellationToken.Null));

You should use the cancellation tokens as much as possible – they greatly lower the application shutdown time and the risk of the appearance of the ``ThreadAbortException``.