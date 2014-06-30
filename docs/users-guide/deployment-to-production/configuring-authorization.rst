Configuring Dashboard authorization
====================================

By default Hangfire allows access to Dashboard pages only for local requests. In order to give appropriate rights for production users, install the following package:

.. code-block:: powershell

   Install-Package Hangfire.Dashboard.Authorization

And configure authorization filters in the :doc:`OWIN bootstrapper's <../getting-started/owin-bootstrapper>` configuration action:

.. code-block:: c#

   using Hangfire.Dashboard;

   app.UseHangfire(config =>
   {
       config.UseAuthorizationFilters(new AuthorizationFilter
       {
           Users = "admin, superuser", // allow only specified users
           Roles = "admins" // allow only specified roles
       });

       // or

       config.UseAuthorizationFilters(
           new ClaimsBasedAuthorizationFilter("hangfire", "access"));
   });

Or implement your own authorization filter:

.. code-block:: c#
    
    using Hangfire.Dashboard;

    public class MyRestrictiveAuthorizationFilter : IAuthorizationFilter
    {
         public bool Authorize(IOwinContext context)
         {
             return false;
         }
    }