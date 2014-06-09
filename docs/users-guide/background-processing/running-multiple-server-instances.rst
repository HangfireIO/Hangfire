Running multiple server instances
==================================

It is possible to run multiple server instances inside a process, machine, or on several machines at the same time. Each server use distributed locks to perform the coordination logic.

Each HangFire Server has a unique identifier that consist of two parts to provide default values for the cases written above. The last part is a process id to handle multiple servers on the same machine. The former part is the *server name*, that defaults to a machine name, to handle unqueness for different machines. Examples: ``server1:9853``, ``server1:4531``, ``server2:6742``.

Since the defaults values provide uniqueness only on a process level, you should to handle it manually, if you want to run different server instances inside the same process:

.. code-block:: c#

    var options = new BackgroundJobServerOptions
    {
        ServerName = String.Format(
            "{0}.{1}",
            Environment.MachineName,
            Guid.NewGuid().ToString())
    };

    var server = new BackgroundJobServer(options);
