Mass Email Tutorial
====================

Programming is an awesome thing. Until you release your app into production. Users provide incorrect data, everything is failing: applications, networks, storages – you have to write 10 error handling code lines for just one line.

Mass email is an example of such thing. I always tried to postpone this feature as long as possible. It is like a nightmare when you have no reliable tools to handle this. Consider you want to send notification emails to all conversation participants on comment creation in ASP.NET MVC application:

.. code-block:: c#

  public ActionResult Create(Comment model)
  {
      if (ModelState.IsValid)
      {
          _db.Comments.Add(model);
          _db.SaveChanges();

          SendNotifications(model);

          return RedirectToAction("Index");
      }

      return View(model);
  }

And the code for the ``SendNotifications`` method:

.. code-block:: c#

  public void SendNotifications(Comment comment)
  {
      var addresses = _db.Comments
          .Select(x => x.Email)
          .Distinct()
          .ToList();

      foreach (var address in addresses)
      {
          var email = new CommentEmail();
          email.To = address;
          email.Comment = comment;

          email.Send();
      }
  }

Simple, elegant, wrong. Consider we are sending 100 emails. We successfully sent 50, but but before we sent 51\ :sup:`st`, remote SMTP server became unavailable. Since our code does not contain any exception handling logic, 50 participants will never receive a notification.

What we can do if our users expect to see a notification sooner or later? We can add retrying logic!

.. code-block:: c#

    // SendNotification method, foreach block
    Exception exception;

    do
    {
        exception = null;
        try
        {
            _service.Send(email);
        }
        catch (SmtpException ex)
        {
            exception = ex;
            Thread.Sleep(<SomeTime>);
        }
    } while (exception != null);

But what if SMTP server will go to ready-to-service state only after 2 hours? Remember that ``SendNotification`` method is running from controller action method that handles a user request. Your user is waiting... for 2 hours... great! Funny thing is that if we limit the retry count, we'll face the initial problem – 50 users will never receive their notifications. 

I also omitted the problem that SmtpException is not always describes transient exception – sometimes SMTP server will always refuse your connection (bad credentials and so on). In this case you should provide additional logic, but I don't want to complicate the topic even more.

So, we can not send mass emails in request processing scope. We need to somehow pass this into background and process a request as fast as possible. Let's start with using ThreadPool threads via TPL.

.. code-block:: c#

    public ActionResult Create(Comment model)
    {
        if (ModelState.IsValid)
        {
            _db.Comments.Add(model);
            _db.SaveChanges();

            Task.Run(() => SendNotifications(model));

            return RedirectToAction("Index");
        }

        return View(model);
    }

We are firing the task, and immediately returning from action. This technique is called fire-and-forget tasks, and it is another evil for ASP.NET application. Consider you are waiting for SMTP-server that will serve you only after two hours. And you decide to switch current SMTP server to another one that causes ASP.NET to initiate application recycling process (or your application becomes idle, or you perform re-deploy, or recycling has been scheduled, etc., etc.). Bye-bye, notifications!

If you think that this happens too rarely, consider that your recycling process started when your application was sending 200 emails and each send takes about 200ms. Even if you use ``IRegisteredObject`` interface, and tell ASP.NET to wait until you finish your processing, it will abort your task after shutdown timeout expired (30s by default). Some letters will never reach their destination.

You should make Windows Service or using recurring task that scans email table with some interval.