OWIN bootstrapper
==================

In OWIN based web application frameworks, such as `ASP.NET MVC <http://www.asp.net/mvc>`_, `FubuMVC <http://fubu-project.org>`_, `Nancy <http://nancyfx.org>`_, `ServiceStack <https://servicestack.net>`_ and many others, you can use :doc:`OWIN bootstrapper <owin-bootstrapper>` methods to simplify the configuration task.

Adding OWIN Startup class
--------------------------

.. note::

   If your project already have the OWIN Startup class (for example if you have SignalR installed), go to the next section.

`OWIN Startup class <http://www.asp.net/aspnet/overview/owin-and-katana/owin-startup-class-detection>`_ is intended to keep web application bootstrap logic in a single place. In Visual Studio 2013 you can add it by right clicking on the project and choosing the *Add / OWIN Startup Class* menu item.

If you have Visual Studio 2012 or earlier, just create a regular class in the root folder of your application, name it ``Startup`` and place the following contents:

.. code-block:: c#

    using HangFire;
    using HangFire.SqlServer;
    using Microsoft.Owin;
    using Owin;

    [assembly: OwinStartup(typeof(MyWebApplication.Startup))]

    namespace MyWebApplication
    {
        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                /* configuration goes here */
            }
        }
    }

Configuring Hangfire
---------------------

Hangfire provides an extension method for the ``IAppBuilder`` interface called ``UseHangfire`` – an entry point to the configuration. :doc:`Storage <../storage-configuration/index>`, :doc:`Job activator <../background-methods/using-ioc-containers>`, :doc:`Authorization filters <../deployment-to-production/configuring-authorization>`, :doc:`Job filters <../extensibility/using-job-filters>` can be configured here, check the available methods through the intellisence. Job storage is the only required configuration option, all others are optional.

.. note::

   Prefer to use the ``UseServer`` method over manual ``BackgroundJobServer`` instantiation to process background jobs inside a web application. The method registers a handler of the application's shutdown event to perform the :doc:`graceful shutdown <../background-methods/using-cancellation-tokens>` for your jobs. 

.. code-block:: c#

   public void Configure(IAppBuilder app)
   {
       app.UseHangfire(config => 
       {
           // Basic setup required to process background jobs.
           config.UseSqlServerStorage("<your connection string or its name>");
           config.UseServer();
       });
   }

The ``UseHangfire`` method also registers the *Hangfire Dashboard* middleware at the ``http://<your-app>/hangfire`` default url (but you can change it).