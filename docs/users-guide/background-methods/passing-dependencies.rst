Passing dependencies
=======================

In almost every job you'll want to use other classes of your application to perform different work and keep your code clean and simple. Let's call these classes as *dependencies*. How to pass these dependencies to methods that will be called in background?

When you are calling static methods in background, you are restricted only to the static context of your application, and this requires you to use the following patterns of obtaining dependencies:

* Manual dependency instantiation through the ``new`` operator
* `Service location <http://en.wikipedia.org/wiki/Service_locator_pattern>`_
* `Abstract factories <http://en.wikipedia.org/wiki/Abstract_factory_pattern>`_ or `builders <http://en.wikipedia.org/wiki/Builder_pattern>`_
* `Singletons <http://en.wikipedia.org/wiki/Singleton_pattern>`_

However, all of these patterns greatly complicate the unit testability aspect of your application. To fight with this issue, Hangfire allows you to call instance methods in background. Consider you have the following class that uses some kind of ``DbContext`` to access the database, and ``EmailService`` to send emails.

.. code-block:: c#

    public class EmailSender
    {
        public void Send(int userId, string message) 
        {
            var dbContext = new DbContext();
            var emailService = new EmailService();

            // Some processing logic
        }
    }

To call the ``Send`` method in background, use the following override of the ``Enqueue`` method (other methods of ``BackgroundJob`` class provide such overloads as well):

.. code-block:: c#

   BackgroundJob.Enqueue<EmailSender>(x => x.Send(13, "Hello!"));

When a worker determines that it need to call an instance method, it creates the instance of a given class first using the current ``JobActivator`` class instance. By default, it uses the ``Activator.CreateInstance`` method that can create an instance of your class using **its default constructor**, so let's add it:

.. code-block:: c#

   public class EmailSender
   {
       private IDbContext _dbContext;
       private IEmailService _emailService;

       public EmailSender()
       {
           _dbContext = new DbContext();
           _emailService = new EmailService();
       } 

       // ...
   }

If you want the class to be ready for unit testing, consider to add constructor overload, because the **default activator can not create instance of class that has no default constructor**:

.. code-block:: c#

    public class EmailSender
    {
        // ...

        public EmailSender()
            : this(new DbContext(), new EmailService())
        {
        }

        internal EmailSender(IDbContext dbContext, IEmailService emailService)
        {
            _dbContext = dbContext;
            _emailService = emailService;
        }
    }

If you are using IoC containers, such as Autofac, Ninject, SimpleInjector and so on, you can remove the default constructor. To leard how to do this, proceed to the next section.