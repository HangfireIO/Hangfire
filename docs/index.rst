HangFire Documentation
=======================

Incredibly easy way to perform **fire-and-forget**, **delayed** and **recurring jobs** inside **ASP.NET applications**. No Windows Service / Task Scheduler required. Backed by Redis, SQL Server, SQL Azure or MSMQ.

HangFire provides unified programming model to handle background tasks in a **reliable way** and run them on shared hosting, dedicated hosting or in cloud. You can start with a simple setup and grow computational power for background jobs with time for these scenarios:

- mass notifications/newsletter;
- batch import from xml, csv, json;
- creation of archives;
- firing off web hooks;
- deleting users;
- building different graphs;
- image/video processing;
- purge temporary files;
- recurring automated reports;
- database maintenance;
- *…and so on.*

HangFire is a .NET Framework alternative to `Sidekiq <http://sidekiq.org>`_, `Resque <https://github.com/resque/resque>`_, `delayed_job <https://github.com/collectiveidea/delayed_job>`_.

.. image:: http://hangfire.io/img/succeeded-job.png

Contents
---------

.. toctree::
   :maxdepth: 3

   pages
   quickstart
   features
   advfeatures
   tutorials/index
   users-guide/index
