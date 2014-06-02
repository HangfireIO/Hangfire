Processing background jobs
===========================

Background job processing is performed by HangFire Server component that is exposed as the ``BackgroundJobServer`` class and its derived class ``AspNetBackgroundJobServer``. To start the background job processing, you need to start it:

.. code-block:: c#

   var server = new BackgroundJobServer();
   server.Start();

Since ASP.NET should know about all background threads to provide graceful shutdown capabilities for them, there is another version of server class – ``AspNetBackgroundJobServer``. It implements the ``IRegisteredObject`` interface and registers itself in the hosting environment.

.. code-block:: c#

   var server = new AspNetBackgroundJobServer();
   server.Start();

.. note::

   If you installed HangFire through the ``HangFire`` NuGet package, these lines of code already added for you with the ``HangFireConfig.cs`` class.
