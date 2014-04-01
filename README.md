HangFire 
=========

[![Build status](https://ci.appveyor.com/api/projects/status/qejwc7kshs1q75m4)](https://ci.appveyor.com/project/odinserj/hangfire) 

* [HangFire Site](http://hangfire.io)
* [HangFire Documentation](http://docs.hangfire.io)
* [HangFire NuGet Packages](https://www.nuget.org/packages?q=hangfire)

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

Just wrap your long-running process to a method and instruct HangFire to create a **background job** based on this method. All backround jobs are being saved to a **persistent storage** ([SQL Server](http://www.microsoft.com/sql‎) or [Redis](http://redis.io)) and performed on a dedicated **worker thread** in a reliable way inside or outside of your ASP.NET application.

HangFire is a .NET Framework alternative to [Resque](https://github.com/resque/resque), [Sidekiq](http://sidekiq.org), [delayed_job](https://github.com/collectiveidea/delayed_job). 

Related Projects
-----------------

* [HangFire.Autofac](https://github.com/odinserj/HangFire.Autofac)
* [HangFire.Ninject](https://github.com/odinserj/HangFire.Ninject)

Contributing
-------------

Open-source projects are developing more smoothly when all discussions are held in public. If you have **any** questions or suggestions, please open GitHub [issues](https://github.com/odinserj/HangFire/issues), mention [@hangfire_net](https://twitter.com/hangfire_net) on Twitter or simple ask a question on a documentation page.

Unfortunately, I can't do all the things at a time. And I appreciate any help related to the project:

* Code contributions, bug fixes.
* Spelling and grammar errors fixes.
* Web interface improvements.
* Documentation clarification.
* Code review.

To make a contribution, please fork a project, do the work and make a [pull-request](https://github.com/odinserj/HangFire/pulls).

License
--------

Copyright © 2013-2014 Sergey Odinokov.

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with this program.  If not, see [http://www.gnu.org/licenses/](http://www.gnu.org/licenses).
