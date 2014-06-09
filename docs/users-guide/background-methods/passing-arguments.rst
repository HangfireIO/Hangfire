Passing arguments
==================

You can pass additional data to your background jobs as a regular method arguments. I'll write the following line once again (hope it hasn't bothered you):

.. code-block:: c#

   BackgroundJob.Enqueue(() => Console.WriteLine("Hello, world!"));

As in a regular method call, its arguments will be available for the ``Console.WriteLine`` method during the background job performance. But since they are used in a expression tree and being serialized, they differ from regular arguments a bit. 

Expression-tree based syntax may impose additional restrictions to the argument values, but thanks to ASP.NET MVC team for their ``CachedExpressionCompiler`` class available with Apache 2.0 license, that makes available to use almost all expression types as arguments: constant, unary, binary, method call, conditional, parameter, etc.

Next restrictions apply to parameter modifiers, and you **can not use** output parameters (``out`` keyword) and parameters passed by reference (``ref`` keyword). They do not make sense to methods that are being called in the background.

And the final restrictions apply to the parameter types. Arguments are being serialized to invariant string using the corresponding ``TypeConverter`` class. Most of simple types have their ``TypeConverter`` implementation available out-of-the-box: numeric types, Boolean, String, DateTime, enum types, TimeSpan, etc. See the complete hierarchy `here <http://msdn.microsoft.com/en-us/library/system.componentmodel.typeconverter(v=vs.110).aspx#inheritanceContinued>`_.

But custom types, arrays, or other collections **can not be converted to string by default**. You should either write `custom type converter <http://www.codeproject.com/Articles/10235/Type-converters-your-friendly-helpers>`_ or use shared storage (for example, SQL Server) to pass identifiers instead.