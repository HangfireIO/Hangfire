Code Syntax Highlighting
=========================

.. warning::

   This is a draft version of tutorial. Please, follow `@hangfire_net
   <https://twitter.com/hangfire_net>`_ on Twitter to receive tutorial update notifications.

This tutorial uses **Visual Studio 2012** with `Web Tools 2013 for Visual Studio 2012
<http://www.asp.net/visual-studio/overview/2012/aspnet-and-web-tools-20131-for-visual-studio-2012>`_ installed, but it can be built either with Visual Studio 2013. Tutorial source code is available on `GitHub
<https://github.com/odinserj/HangFire.Highlighter>`_.

.. contents:: Table of Contents
   :local:
   :depth: 2

Overview
---------

Let's start with a simple example. Consider you are building a code snippet gallery web application like `GitHub Gists
<http://gist.github.com>`_ and want to implement the syntax highlighting feature. To improve user experience, you are also want it to work even if a user disabled JavaScript in her browser.

To support this scenario and to reduce the project development time, you choosed to use a web service for syntax highlighting, such as http://pygments.appspot.com or http://www.hilite.me.

.. note::

   Although there are some syntax highlighter libraries for .NET, we are using web services just to show some pitfalls regarding to their use in applications. You can substitute this example with real-world scenario, like using the http://postageapp.com service.

Setting up the project
-----------------------

.. tip::

   This section contains steps to prepare the project. However, if you are not want to do the boring stuff, you can clone the repo and go straight to the `Hiliting the code!` section.

Creating a project
^^^^^^^^^^^^^^^^^^^

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
^^^^^^^^^^^^^^^^^

We'll use `SQL Server 2012 Express (or later)
<http://www.microsoft.com/sqlserver/en/us/editions/express.aspx>`_ to store code snippets and `Entity Framework
<http://msdn.microsoft.com/ru-ru/data/ef.aspx>`_ to access our database.

Installing Entity Framework
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Open the `Package Manager Console
<https://docs.nuget.org/docs/start-here/using-the-package-manager-console>`_ window and type:

.. code-block:: powershell

   Install-Package EntityFramework

After install the package, create a new class in the ``Models`` folder and name it ``HighlighterDbContext``:

.. code-block:: c#

   using System.Data.Entity;

   namespace HangFire.Highlighter.Models
   {
       public class HighlighterDbContext : DbContext
       {
           public HighlighterDbContext() : base("HighlighterDb")
           {
           }
       }
   }

Please note, that we are using undefined yet connection string name ``HighlighterDb``. So, lets add it to the ``web.config`` file just after the ``</configSections>`` tag:

.. code-block:: xml

   <connectionStrings>
     <add name="HighlighterDb" connectionString="Server=.\sqlexpress; Database=HangFire.Highlighter; Trusted_Connection=True;" providerName="System.Data.SqlClient" />
   </connectionStrings>

And enable Entity Framework Code First Migrations by typing in your Package Manager Console window the following command:

.. code-block:: powershell

   Enable-Migrations

Adding code snippet model
~~~~~~~~~~~~~~~~~~~~~~~~~~

It's time to add the most valuable class in the application. Create the ``CodeSnippet`` class in the ``Models`` folder with the following code:

.. code-block:: c#

   using System;
   using System.ComponentModel.DataAnnotations;
   using System.Web.Mvc;

   namespace HangFire.Highlighter.Models
   {
       public class CodeSnippet
       {
           public int Id { get; set; }

           [Required, AllowHtml, Display(Name = "C# source")]
           public string SourceCode { get; set; }
           public string HighlightedCode { get; set; }

           public DateTime CreatedAt { get; set; }
           public DateTime? HighlightedAt { get; set; }
       }
   }

   // Don't forget to include the following property in the 
   // `HighlighterDbContext` class:
   public DbSet<CodeSnippet> CodeSnippets { get; set; }

Then add a database migration and run it by typing the following commands into the Package Manager Console window:

.. code-block:: powershell

   Add-Migration AddCodeSnippet
   Update-Database

Our database is ready to use!

Creating actions and views
^^^^^^^^^^^^^^^^^^^^^^^^^^^

Now its time to breathe life into our project. Please, modify the following files as described.

.. code-block:: c#

  // Controllers/HomeController.cs

  using System;
  using System.Linq;
  using System.Web.Mvc;
  using HangFire.Highlighter.Models;

  namespace HangFire.Highlighter.Controllers
  {
      public class HomeController : Controller
      {
          private readonly HighlighterDbContext _db = new HighlighterDbContext();

          public ActionResult Index()
          {
              return View(_db.CodeSnippets.ToList());
          }

          public ActionResult Details(int id)
          {
              var snippet = _db.CodeSnippets.Find(id);
              return View(snippet);
          }

          public ActionResult Create()
          {
              return View();
          }

          [HttpPost]
          public ActionResult Create([Bind(Include="SourceCode")] CodeSnippet snippet)
          {
              if (ModelState.IsValid)
              {
                  snippet.CreatedAt = DateTime.UtcNow;

                  _db.CodeSnippets.Add(snippet);
                  _db.SaveChanges();

                  return RedirectToAction("Index");
              }

              return View(snippet);
          }

          protected override void Dispose(bool disposing)
          {
              if (disposing)
              {
                  _db.Dispose();
              }
              base.Dispose(disposing);
          }
      }
  }

.. code-block:: html

  @* ~/Views/Index.cshtml *@

  @model IEnumerable<HangFire.Highlighter.Models.CodeSnippet>
  @{ ViewBag.Title = "Snippets"; }

  <h2>Snippets</h2>

  <p><a class="btn btn-primary" href="@Url.Action("Create")">Create Snippet</a></p>
  <table class="table">
      <tr>
          <th>Code</th>
          <th>Created At</th>
          <th>Highlighted At</th>
      </tr>

      @foreach (var item in Model)
      {
          <tr>
              <td>
                  <a href="@Url.Action("Details", new { id = item.Id })">@Html.Raw(item.HighlightedCode)</a>
              </td>
              <td>@item.CreatedAt</td>
              <td>@item.HighlightedAt</td>
          </tr>
       }
  </table>

.. code-block:: html

  @* ~/Views/Create.cshtml *@

  @model HangFire.Highlighter.Models.CodeSnippet
  @{ ViewBag.Title = "Create a snippet"; }

  <h2>Create a snippet</h2>

  @using (Html.BeginForm())
  {
      @Html.ValidationSummary(true)

      <div class="form-group">
          @Html.LabelFor(model => model.SourceCode)
          @Html.ValidationMessageFor(model => model.SourceCode)
          @Html.TextAreaFor(model => model.SourceCode, new { @class = "form-control", style = "min-height: 300px;", autofocus = "true" })
      </div>

      <button type="submit" class="btn btn-primary">Create</button>
      <a class="btn btn-default" href="@Url.Action("Index")">Back to List</a>
  }

.. code-block:: html

  @* ~/Views/Details.cshtml *@

  @model HangFire.Highlighter.Models.CodeSnippet
  @{ ViewBag.Title = "Details"; }

  <h2>Snippet <small>#@Model.Id</small></h2>

  <div>
      <dl class="dl-horizontal">
          <dt>@Html.DisplayNameFor(model => model.CreatedAt)</dt>
          <dd>@Html.DisplayFor(model => model.CreatedAt)</dd>
          <dt>@Html.DisplayNameFor(model => model.HighlightedAt)</dt>
          <dd>@Html.DisplayFor(model => model.HighlightedAt)</dd>
      </dl>
      
      <div class="clearfix"></div>
  </div>

  <div>@Html.Raw(Model.HighlightedCode)</div>

Hiliting the code!
-------------------

http://hilite.me service provides HTTP API to perform highlighting work. We'll consume it with the ``Microsoft.Net.Http`` package:

.. code-block:: powershell

   Install-Package Microsoft.Net.Http