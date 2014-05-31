Using IoC containers
=====================

As I said in the :doc:`previous section <passing-dependencies>` HangFire uses the ``JobActivator`` class to instantiate the target types before invoking instance methods. You can override its behavior to perform more complex logic on a type instantiation. For example, you can tell it to use IoC container that is being used in your project:

.. code-block:: c#

   public class ContainerJobActivator : JobActivator
   {
       private IContainer _container;

       public ContainerJobActivator(IContainer container)
       {
           _container = container;
       }

       public override object ActivateJob(Type type)
       {
           return _container.Resolve(type);
       }
   }

Then, you need to register it as a current job activator:

.. code-block:: c#

   // Somewhere in bootstrap logic, for example in the Global.asax.cs file
   var container = new Container();

   JobActivator.Current = new ContainerJobActivator(container);

To simplify the initial installation, there are some integration  packages already available on NuGet:

* `HangFire.Autofac <https://www.nuget.org/packages/HangFire.Autofac/>`_
* `HangFire.Ninject <https://www.nuget.org/packages/HangFire.Ninject/>`_
* `HangFire.SimpleInjector <https://www.nuget.org/packages/HangFire.SimpleInjector/>`_
* `HangFire.Windsor <https://www.nuget.org/packages/HangFire.Windsor/>`_

Please, note that request information is not available during the instantiation of a target type. If you register your dependencies in a request scope (``InstancePerHttpRequest`` in Autofac, ``InRequestScope`` in Ninject and so on), an exception will be thrown during the job activation process.

So, **the entire dependency graph should be available**. Either register additional services without using the request scope, or use separate instance of container if your IoC container does not support dependency registrations for multiple scopes.