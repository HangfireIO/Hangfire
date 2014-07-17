Sending Mail in Background with ASP.NET MVC
============================================

.. contents:: Table of Contents
   :local:
   :depth: 2

Let's start with a simple example: you are building your own blog using ASP.NET MVC and want to receive an email notification about each posted comment. We will use the simple but awesome `Postal <http://aboutcode.net/postal/>`_ library to send emails. 

.. tip::

   I've prepared a simple application that has only comments list, you can `download its sources <https://github.com/odinserj/Hangfire.Mailer/releases/tag/vBare>`_ to start work on tutorial.

You already have a controller action that creates a new comment, and want to add the notification feature.

.. code-block:: c#

    // ~/HomeController.cs

    [HttpPost]
    public ActionResult Create(Comment model)
    {
        if (ModelState.IsValid)
        {
            _db.Comments.Add(model);
            _db.SaveChanges();
        }

        return RedirectToAction("Index");
    }

Installing Postal
------------------

First, install the ``Postal`` NuGet package:

.. code-block:: powershell

   Install-Package Postal

Then, create ``~/Models/NewCommentEmail.cs`` file with the following contents:

.. code-block:: c#

    using Postal;

    namespace Hangfire.Mailer.Models
    {
        public class NewCommentEmail : Email
        {
            public string To { get; set; }
            public string UserName { get; set; }
            public string Comment { get; set; }
        }
    }

Create a corresponding template for this email by adding the ``~/Views/Emails/NewComment.cshtml`` file:

.. code-block:: text

    @model Hangfire.Mailer.Models.NewCommentEmail
    To: @Model.To
    From: mailer@example.com
    Subject: New comment posted

    Hello, 
    There is a new comment from @Model.UserName:

    @Model.Comment

    <3

And call Postal to sent email notification from the ``Create`` controller action:

.. code-block:: c#

    [HttpPost]
    public ActionResult Create(Comment model)
    {
        if (ModelState.IsValid)
        {
            _db.Comments.Add(model);
            _db.SaveChanges();

            var email = new NewCommentEmail
            {
                To = "yourmail@example.com",
                UserName = model.UserName,
                Comment = model.Text
            };

            email.Send();
        }

        return RedirectToAction("Index");
    }

Then configure the delivery method in the ``web.config`` file (by default, tutorial source code uses ``C:\Temp`` directory to store outgoing mail):

.. code-block:: xml

  <system.net>
    <mailSettings>
      <smtp deliveryMethod="SpecifiedPickupDirectory">
        <specifiedPickupDirectory pickupDirectoryLocation="C:\Temp\" />
      </smtp>
    </mailSettings>
  </system.net>

That's all. Try to create some comments and you'll see notifications in the pickup directory.

Further considerations
-----------------------

But why should a user  wait until the notification was sent? There should be some way to send emails asynchronously, in the background, and return a response to the user as soon as possible. 

Unfortunately, `asynchronous <http://www.asp.net/mvc/tutorials/mvc-4/using-asynchronous-methods-in-aspnet-mvc-4>`_ controller actions `does not help <http://blog.stephencleary.com/2012/08/async-doesnt-change-http-protocol.html>`_ in this scenario, because they do not yield response to the user while waiting for the asynchronous operation to complete. They only solve internal issues related to thread pooling and application capacity.

There are `great problems <http://blog.stephencleary.com/2012/12/returning-early-from-aspnet-requests.html>`_ with background threads also. You should use Thread Pool threads or custom ones that are running inside ASP.NET application with care – you can simply lose your emails during the application recycle process (even if you register an implementation of the ``IRegisteredObject`` interface in ASP.NET).

And you are unlikely to want to install external Windows Services or use Windows Scheduler with a console application to solve this simple problem (we are building a personal blog, not an e-commerce solution).

Installing Hangfire
--------------------

To be able to put tasks into the background and not lose them during application restarts, we'll use `Hangfire <http://hangfire.io>`_. It can handle background jobs in a reliable way inside ASP.NET application without external Windows Services or Windows Scheduler.

.. code-block:: powershell

   Install-Package Hangfire

Hangfire uses SQL Server or Redis to store information about background jobs. So, let's configure it. Add or update the OWIN Startup class as :doc:`written here <../users-guide/getting-started/owin-bootstrapper>`.

.. code-block:: c#

   public void Configure(IAppBuilder app)
   {
       app.UseHangfire(config =>
       {
           app.UseSqlServerStorage("MailerDb");
           app.UseServer();
       });
   }

The ``SqlServerStorage`` class will install all database tables automatically on application start-up (but you are able to do it manually).

Now we are ready to use Hangfire. It asks us to wrap a piece of code that should be executed in background in a public method.

.. code-block:: c#

    [HttpPost]
    public ActionResult Create(Comment model)
    {
        if (ModelState.IsValid)
        {
            _db.Comments.Add(model);
            _db.SaveChanges();

            BackgroundJob.Enqueue(() => NotifyNewComment(model.Id));
        }

        return RedirectToAction("Index");
    }

Note, that we are passing a comment identifier instead of a full comment – Hangfire should be able to serialize all method call arguments to string values. The default serializer does not know anything about our ``Comment`` class. Furthermore, the integer identifier takes less space in serialized form than the full comment text.

Now, we need to prepare the ``NotifyNewComment`` method that will be called in the background. Note that ``HttpContext.Current`` is not available in this situation, but Postal library can work even `outside of ASP.NET request <http://aboutcode.net/postal/outside-aspnet.html>`_. But first install another package (that is needed for Postal 0.9.2, see `the issue <https://github.com/andrewdavey/postal/issues/68>`_).

.. code-block:: powershell

   Install-Package RazorEngine

.. code-block:: c#

    public static void NotifyNewComment(int commentId)
    {
        // Prepare Postal classes to work outside of ASP.NET request
        var viewsPath = Path.GetFullPath(HostingEnvironment.MapPath(@"~/Views/Emails"));
        var engines = new ViewEngineCollection();
        engines.Add(new FileSystemRazorViewEngine(viewsPath));

        var emailService = new EmailService(engines);

        // Get comment and send a notification.
        using (var db = new MailerDbContext())
        {
            var comment = db.Comments.Find(commentId);

            var email = new NewCommentEmail
            {
                To = "yourmail@example.com",
                UserName = comment.UserName,
                Comment = comment.Text
            };

            emailService.Send(email);
        }
    }

This is a plain C# static method. We are creating an ``EmailService`` instance, finding the desired comment and sending a mail with Postal. Simple enough, especially when compared to a custom Windows Service solution.

That's all! Try to create some comments and see the ``C:\Temp`` path. You also can check your background jobs at ``http://<your-app>/hangfire``. If you have any questions, you are welcome to use the comments form below.

.. note::

   If you experience assembly load exceptions, please, please delete the following sections from the ``web.config`` file (I forgot to do this, but don't want to re-create the repository):

   .. code-block:: xml

      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="Common.Logging" publicKeyToken="af08829b84f0328e" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-2.2.0.0" newVersion="2.2.0.0" />
      </dependentAssembly>

Automatic retries
------------------

When the ``emailService.Send`` method throws an exception, HangFire will retry it automatically after a delay (that is increased with each attempt). The retry attempt count is limited (3 by default), but you can increase it. Just apply the ``AutomaticRetryAttribute`` to the ``NotifyNewComment`` method:

.. code-block:: c#

   [AutomaticRetry(20)]
   public static void NotifyNewComment(int commentId)
   {
       /* ... */
   }

Logging
--------

You can log cases when the maximum number of retry attempts has been exceeded. Try to create the following class:

.. code-block:: c#

    public class LogFailureAttribute : JobFilterAttribute, IApplyStateFilter
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
            var failedState = context.NewState as FailedState;
            if (failedState != null)
            {
                Logger.Error(
                    String.Format("Background job #{0} was failed with an exception.", context.JobId), 
                    failedState.Exception);
            }
        }

        public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
        {
        }
    }

And add it:

Either globally by calling the following method at application start:

.. code-block:: c#

   GlobalJobFilters.Filters.Add(new LogFailureAttribute());

Or locally by applying the attribute to a method:

.. code-block:: c#

   [LogFailure]
   public static void NotifyNewComment(int commentId)
   {
       /* ... */
   }

Fix-deploy-retry
-----------------

If you made a mistake in your ``NotifyNewComment`` method, you can fix it and restart the failed background job via the web interface. Try it:

.. code-block:: c#

   // Break background job by setting null to emailService:
   var emailService = null;

Compile a project, add a comment and go to the web interface by typing ``http://<your-app>/hangfire.axd``. Exceed all automatic attempts, then fix the job, restart the application, and click the ``Retry`` button on the *Failed jobs* page.

Preserving current culture
---------------------------

If you set a custom culture for your requests, Hangfire will store and set it during the performance of the background job. Try the following:

.. code-block:: c#

   // HomeController/Create action
   Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("es-ES");
   BackgroundJob.Enqueue(() => NotifyNewComment(model.Id));

And check it inside the background job:

.. code-block:: c#

    public static void NotifyNewComment(int commentId)
    {
        var currentCultureName = Thread.CurrentThread.CurrentCulture.Name;
        if (currentCultureName != "es-ES")
        {
            throw new InvalidOperationException(String.Format("Current culture is {0}", currentCultureName));
        }
        // ...
