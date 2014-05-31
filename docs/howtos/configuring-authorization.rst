Configuring HangFire Monitor authorization
===========================================

After installing the ``HangFire.Web`` package (it's a dependency of the ``HangFire`` package)., the ``web.config`` file of your application is being changed to include *HangFire Monitor* HTTP handler to see what's going on with your background jobs.

By default, you can access this UI only from a local machine for security reasons described `here <http://odinserj.net/2014/05/02/hangfire-0.8-released/#toc_0>`_. This policy makes the Monitor absolutely useless for production environments, but you can change this behavior through changing the ``hangfire:EnableRemoteMonitorAccess`` setting in your ``web.config`` file:

.. code-block:: xml

  <appSettings>
    <add key="hangfire:EnableRemoteMonitorAccess" value="true"/>
    ...
  </appSettings>

However, if you do this, anyone will be able to access this internal page and sooner or later some dishonorable users may use it in their dishonorable interests. To prevent this, you should install (if it's not installed yet) the Role Provider implementation (`MembershipProvider <http://msdn.microsoft.com/en-us/library/system.web.security.membershipprovider.aspx>`_, `SimpleMembershipProvider <http://msdn.microsoft.com/ru-ru/library/webmatrix.webdata.simplemembershipprovider(v=vs.111).aspx>`_ or new `ASP.NET Identity <http://www.asp.net/identity>`_), and choose the roles you want to give access to in your ``web.config`` using standard ASP.NET authorization configuration.

.. code-block:: xml

  <location path="hangfire.axd" inheritInChildApplications="false">
    <system.web>
      <authorization>
        <allow roles="Administrator" />
        <deny users="*" />
      </authorization>
    </system.web>
    ...
  </location>
