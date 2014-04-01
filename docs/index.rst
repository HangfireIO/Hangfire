HangFire Documentation
=======================

HangFire gives you a simple way to kick off **long-running processes** from the **ASP.NET request processing pipeline**. Asynchronous, transparent, reliable, efficient processing. No Windows service/ Task Scheduler required. Even ASP.NET is not required.

Improve the responsiveness of your web application. Do not force your users to wait when the application performs the following tasks:

- mass notifications/newsletter;
- batch import from xml, csv, json;
- creation of archives;
- firing off web hooks;
- deleting users;
- building different graphs;
- image processing;
- *…and so on.*

Just wrap your long-running process to a method and instruct HangFire to create a **background job** based on this method. All backround jobs are being saved to a **persistent storage** (`SQL Server
<http://www.microsoft.com/sql‎>`_ or `Redis
<http://redis.io>`_) and performed on a dedicated **worker thread** in a reliable way inside or outside of your ASP.NET application.

HangFire is a .NET Framework alternative to `Resque
<https://github.com/resque/resque>`_, `Sidekiq
<http://sidekiq.org>`_, `delayed_job
<https://github.com/collectiveidea/delayed_job>`_.

Contents
---------

.. toctree::
   :maxdepth: 2

   quickstart
   features
   advfeatures
