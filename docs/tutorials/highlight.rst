Code Highlighting
==================

.. warning::

   This is a draft version of tutorial. Please, follow `@hangfire_net
   <https://twitter.com/hangfire_net>`_ on Twitter to receive tutorial update notifications.

This tutorial uses **Visual Studio 2012** with `Web Tools 2013 for Visual Studio 2012
<http://www.asp.net/visual-studio/overview/2012/aspnet-and-web-tools-20131-for-visual-studio-2012>`_ installed, but it can be built either with Visual Studio 2013. Tutorial source code is available on `GitHub
<https://github.com/odinserj/HangFire.Highlighter>`_.

.. contents:: Table of Contents
   :local:

Overview
---------

Let's start with a simple example. Consider you are building a code snippet gallery web application like `GitHub Gists
<http://gist.github.com>`_ and want to implement the syntax highlighting feature. To improve user experience, you are also want it to work even if a user disabled JavaScript in her browser.

To support this scenario and to reduce the project development time, you choosed to use a web service for syntax highlighting, such as http://pygments.appspot.com or http://www.hilite.me.

.. note::

   Although there are some syntax highlighter libraries for .NET, we are using web services just to show some pitfalls regarding to their use in applications. You can substitute this example with real-world scenario, like using the http://postageapp.com service.

Creating a project
-------------------

Let's create an empty ASP.NET MVC 5 application and try to build an awesome app called ``HangFire.Highlighter``:

.. image:: highlighter/newproj.png

Then, scaffold an **MVC 5 Controller - Empty** controller and call it ``HomeController``:

.. image:: highlighter/addcontrollername.png

Our controller looks like:

.. code-block:: c#

   public class HomeController : Controller
   {
       public ActionResult Index()
       {
           return View();
       }
   }

Now we need to show something, so let's scaffold an **empty view** for the ``Index`` action:

.. image:: highlighter/addview.png

After these steps my solution looks like:

.. image:: highlighter/solutionafterview.png

Defining a model
-----------------

We'll use the **Entity Framework 6.1** library to define the model of our application. To install it, type in your `Package Manager Console
<https://docs.nuget.org/docs/start-here/using-the-package-manager-console>`_ window:

.. code-block:: powershell

   Install-Package EntityFramework