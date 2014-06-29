Best practices
===============

Background job processing differ a lot from a regular method invocation. This guide will help you to keep background processing running smoothly and efficiently. The information is given on a basis of `this blog post <http://odinserj.net/2014/05/10/are-your-methods-ready-to-run-in-background/>`_.

Make job arguments small and simple
------------------------------------

Method invocation (i.e. job) is being serialized during the background job creation process. Arguments are also being converted into strings using the `TypeConverter` class. If you have complex entities or large objects, including arrays, it is better to place them into a database, and pass only their identities to background jobs.

Instead this:

.. code-block:: c#

   public void Method(Entity entity) { }

Consider doing this:

.. code-block:: c#

   public void Method(int entityId) { }

Make your background methods reentrant
---------------------------------------

`Reentrancy <https://en.wikipedia.org/wiki/Reentrant_(subroutine)>`_ means that a method can be interrupted in the middle of its execution and then safely called again. The interruption can be caused by different exceptions, and Hangfire will attempt to retry it many times.

You can face with different problems, if you didn't prepared your method to this feature. For example, if you are using email sending background job and experience errors with SMTP service, you can end with multiple letters sent to the single 

Instead this:

.. code-block:: c#

   public void Method()
   {
       _emailService.Send("person@exapmle.com", "Hello!");
   }

Consider doing this:

.. code-block:: c#

   public void Method(int deliveryId)
   {
       if (_emailService.IsNotDelivered(deliveryId))
       {
           _emailService.Send("person@example.com", "Hello!");
           _emailService.SetDelivered(deliveryId);
       }
   }

*To be continued…*