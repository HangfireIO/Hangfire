Processing background jobs
===========================

Background job processing is performed by Hangfire Server component that is exposed as the ``BackgroundJobServer`` class. To start the background job processing, you need to start it:

.. code-block:: c#

   var server = new BackgroundJobServer();
   server.Start();

You should also call the ``Stop`` method to shutdown all background thread gracefully, making :doc:`cancellation tokens <../background-methods/using-cancellation-tokens>` work on shutdown event.

.. code-block:: c#

   server.Stop();

If you want to process background jobs inside a web application, you should use the :doc:`OWIN bootstrapper's <../getting-started/owin-bootstrapper>` ``UseServer`` method instead of manual instantiation of the ``BackgroundJobServer`` class. If you are curious why, you should know that this method registers the callback for ``host.OnAppDisposing`` application event that stops the Server gracefully.

.. code-block:: c#

   app.UseHangfire(config =>
   {
       config.UseServer();
   });

.. warning::

   If you are using custom installation within a web application hosted in IIS, do not forget to install the `Microsoft.Owin.Host.SystemWeb <https://www.nuget.org/packages/Microsoft.Owin.Host.SystemWeb/>`_ package. Otherwise some features, like graceful shutdown may not work.