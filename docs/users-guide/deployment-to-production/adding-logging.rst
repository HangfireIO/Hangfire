Adding logging
===============

Hangfire uses the `Common.Logging <https://github.com/net-commons/common-logging>`_ library to log different events in an abstract way. If your application already have a logging framework installed, you need only to install the corresponding logging adapter and plug it in your application. You can check the list of supported adapters on `NuGet Gallery site <https://www.nuget.org/packages?q=common.logging>`_.

If your application does not have any logging framework installed, you need to choose and install it first. There are different logging frameworks, such as NLog, log4net, etc., but their description should not be in this article.

Hangfire does not procuce much log messages and uses different logging levels to separate different types of messages. All logger names start with the ``Hangfire`` prefix, so you can use wildcarding feature of your logging framework to make Hangfire logging separate from your application logging.

Installing support for NLog
----------------------------

This section is for demonstration purposes only – to show that logging feature is easy to install. Consider you have an application with NLog library that is already configured. You only need to 

1. Install the logging adapter for ``Common.Logging`` library:

  .. code-block:: powershell

     PM> Install-Package Common.Logging.NLog20

2. Configure the installed logging adapter:

  .. code-block:: c#

     var properties = new NameValueCollection();
     properties["configType"] = "INLINE";

     LogManager.Adapter = new NLogLoggerFactoryAdapter(properties);

For more information, please refer to the Common.Logging library's `documentation <http://netcommon.sourceforge.net/documentation.html>`_.

Log level description
----------------------

* **Trace** – for debugging Hangfire itself.
* **Debug** – for know why background processing does not work for you.
* **Info**  – to see that everything is working as expected: *Hangfire was started or stopped*, *Hangfire components performed useful work*. This is the **recommended** level to log.
* **Warn**  – to know about potential problems early: *performance failed, but automatic retry attempt will be made*, *thread abort exceptions*.
* **Error** – to know about problems that may lead to temporary background processing disruptions or problems you should know about: *performance failed, you need either to retry or delete a job manually*, *storage connectivity errors, automatic retry attempt will be made*.
* **Fatal** – to know that background job processing does not work partly or entirely, and requires manual intervention: *storage connectivity errors, retry attempts exceeded*, *different internal issues, such as OutOfMemoryException and so on*.