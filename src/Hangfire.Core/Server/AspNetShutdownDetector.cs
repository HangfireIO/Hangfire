// This file is part of Hangfire. Copyright © 2020 Hangfire OÜ.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal static class AspNetShutdownDetector
    {
        private static readonly TimeSpan CheckForShutdownTimerInterval = TimeSpan.FromMilliseconds(250);
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

#if !NETSTANDARD1_3
        private static int _isInitialized;
        private static bool _isSucceeded;
        // ReSharper disable once NotAccessedField.Local
        private static Thread _checkForShutdownThread;
        private static Func<string> _shutdownReasonFunc;
        private static Func<bool> _checkConfigChangedFunc;
        private static Func<bool> _disposingHttpRuntime;
#endif

        public static bool IsSucceeded =>
#if !NETSTANDARD1_3
            _isSucceeded
#else
            false
#endif
        ;

        public static CancellationToken GetShutdownToken()
        {
#if !NETSTANDARD1_3
            EnsureInitialized();
#endif
            return CancellationTokenSource.Token;
        }

        public static bool DisposingHttpRuntime =>
#if !NETSTANDARD1_3
            _disposingHttpRuntime != null && _disposingHttpRuntime()
#else
            false
#endif
            ;

#if !NETSTANDARD1_3
        private static void EnsureInitialized()
        {
            if (Interlocked.Exchange(ref _isInitialized, 1) != 0) return;

            try
            {
                // Normally when ASP.NET is stopping our web application, IRegisteredObject.Stop
                // method is called for all the registered objects, and it is the recommended
                // way for handling shutdowns when we have some custom background threads.
                // 
                // Hangfire uses OWIN's "host.OnAppDisposing" and "server.OnDispose" keys that
                // provide a cancellation token which is canceled after OWIN's own registered
                // object is stopped, and this method works most of the time. But...

                // Long-running web requests can prevent ASP.NET from stopping the registered
                // objects, because it waits for all the requests to end before shutting down
                // the application. But since .NET Framework 4.5.1, ASP.NET triggers the
                // StopListening event in these cases, so we can listen it and initiate a
                // shutdown once our application stopped to listen for the new requests.
                RegisterForStopListeningEvent(ref _isSucceeded);

                // Overlapped recycles feature is turned on by default in IIS, which cause
                // both old and new application to be active at the same time during app
                // domain recycles, which may cause old servers to process background jobs
                // from the new ones, resulting in exceptions when new methods added.
                // Also during deployments we can get numerous of startup/shutdown attempts,
                // because file updates aren't transactional, and since deploys often touch
                // multiple files.
                // Unfortunately during such deploys registered objects often don't stopped
                // carefully, especially when "autostart providers" feature is used, perhaps
                // due to some race conditions in ASP.NET.
                // After investigating source code of ASP.NET I've found that there's no better
                // solution for this issue other than to check the shutdown reason from time
                // to time.
                InitializeShutdownReason(ref _isSucceeded);

                // This check is based on the HttpRuntime._disposingHttpRuntime field that's
                // modified just before app domain is being unloaded, when new app domain was
                // already created.
                InitializeDisposingHttpRuntime(ref _isSucceeded);

                // And the last method to check for application shutdown is implemented in
                // SignalR and Kudu service by checking the UnsafeIISMethods.MgdHasConfigChanged
                // method. But I was failed to find this method in the recent ASP.NET sources.
                // But nevertheless it may be useful to have it for older versions.
                InitializeMgdHasConfigChanged(ref _isSucceeded);                

                if (_isSucceeded)
                {
                    _checkForShutdownThread = new Thread(CheckForAppDomainShutdown)
                    {
                        Name = "AspNetShutdownDetector",
                        IsBackground = true,
                        Priority = ThreadPriority.AboveNormal
                    };
                }
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                GetLogger().ErrorException("Failed to initialize shutdown triggers for ASP.NET application.", ex);
            }
        }

        private static void CheckForAppDomainShutdown(object state)
        {
            try
            {
                while (!CancellationTokenSource.IsCancellationRequested)
                {
                    if (_checkConfigChangedFunc != null && _checkConfigChangedFunc())
                    {
                        Cancel("UnsafeIISMethods.MgdHasConfigChanged");
                        break;
                    }

                    var shutdownReason = _shutdownReasonFunc?.Invoke();
                    if (shutdownReason != null)
                    {
                        Cancel($"HostingEnvironment.ShutdownReason: {shutdownReason}");
                        break;
                    }

                    if (_disposingHttpRuntime != null && _disposingHttpRuntime())
                    {
                        Cancel("HttpRuntime._disposingHttpRuntime");
                        break;
                    }

                    Thread.Sleep(CheckForShutdownTimerInterval);
                }
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                GetLogger().ErrorException(
                    "An exception occurred while checking for ASP.NET shutdown, will not able to do the checks properly.",
                    ex);
            }
        }

        private static void Cancel(string reason)
        {
            GetLogger().Info($"ASP.NET application is shutting down: {reason}.");

            try
            {
                CancellationTokenSource.Cancel(throwOnFirstException: false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (AggregateException ag)
            {
                GetLogger().ErrorException("One or more exceptions were thrown during app pool shutdown: ", ag);
            }
        }

        private static void RegisterForStopListeningEvent(ref bool success)
        {
            try
            {
                var hostingEnvironmentType = Type.GetType("System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (hostingEnvironmentType == null) return;

                var stopEvent = hostingEnvironmentType.GetEvent("StopListening", BindingFlags.Static | BindingFlags.Public);
                if (stopEvent == null) return;

                stopEvent.AddEventHandler(null, new EventHandler(StopListening));
                GetLogger().Debug("HostingEnvironment.StopListening shutdown trigger initialized successfully.");
                success = true;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                GetLogger().DebugException("Unable to initialize HostingEnvironment.StopListening shutdown trigger", ex);
            }
        }

        private static void StopListening(object sender, EventArgs e)
        {
            Cancel("HostingEnvironment.StopListening");
        }

        private static void InitializeShutdownReason(ref bool success)
        {
            try
            {
                var hostingEnvironment = Type.GetType("System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (hostingEnvironment == null) return;

                var shutdownReason = hostingEnvironment.GetProperty("ShutdownReason", BindingFlags.Static | BindingFlags.Public);
                if (shutdownReason == null) return;

                _shutdownReasonFunc = ShutdownReasonFunc;

                GetLogger().Debug("HostingEnvironment.ShutdownReason shutdown trigger initialized successfully.");
                success = true;

                string ShutdownReasonFunc()
                {
                    try
                    {
                        var shutdownReasonValue = shutdownReason.GetValue(null);

                        if (shutdownReasonValue != null && (int)shutdownReasonValue != 0)
                        {
                            return shutdownReasonValue.ToString();
                        }
                    }
                    catch (Exception ex) when (ex.IsCatchableExceptionType())
                    {
                        GetLogger().TraceException("Unable to call the HostingEnvironment.ShutdownReason property due to an exception.", ex);
                    }

                    return null;
                }
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                GetLogger().DebugException("Unable to initialize HostingEnvironment.ShutdownReason shutdown trigger", ex);
            }
        }

        private static void InitializeMgdHasConfigChanged(ref bool success)
        {
            try
            {
                var type = Type.GetType("System.Web.Hosting.UnsafeIISMethods, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (type == null) return;

                var methodInfo = type.GetMethod("MgdHasConfigChanged", BindingFlags.NonPublic  | BindingFlags.Static);
                if (methodInfo == null) return;

                _checkConfigChangedFunc = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), methodInfo);

                GetLogger().Debug("UnsafeIISMethods.MgdHasConfigChanged shutdown trigger initialized successfully.");
                success = true;
            }
            catch (Exception ex) when (ex.IsCatchableExceptionType())
            {
                GetLogger().DebugException("Unable to initialize UnsafeIISMethods.MgdHasConfigChanged shutdown trigger", ex);
            }
        }

        private static void InitializeDisposingHttpRuntime(ref bool success)
        {
            try
            {
                var type = Type.GetType("System.Web.HttpRuntime, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (type == null) return;

                var theRuntimeInfo = type.GetField("_theRuntime", BindingFlags.NonPublic | BindingFlags.Static);
                if (theRuntimeInfo == null) return;

                var disposingHttpRuntimeInfo = type.GetField("_disposingHttpRuntime", BindingFlags.NonPublic | BindingFlags.Instance);
                if (disposingHttpRuntimeInfo == null) return;

                var theRuntime = CreateGetStaticFieldDelegate<object>(theRuntimeInfo);
                var disposingHttpRuntime = CreateGetFieldDelegate<bool>(disposingHttpRuntimeInfo, type);

                _disposingHttpRuntime = () => disposingHttpRuntime(theRuntime());

                GetLogger().Debug("HttpRuntime._disposingHttpRuntime shutdown trigger initialized successfully.");
                success = true;
            }
            catch (Exception ex)
            {
                GetLogger().DebugException("Unable to initialize HttpRuntime._disposingHttpRuntime shutdown trigger", ex);
            }
        }

        private static Func<T> CreateGetStaticFieldDelegate<T>(FieldInfo fieldInfo)
        {
            var fieldExp = Expression.Field(null, fieldInfo);
            return Expression.Lambda<Func<T>>(fieldExp).Compile();
        }

        private static Func<object, T> CreateGetFieldDelegate<T>(FieldInfo fieldInfo, Type type)
        {
            var instExp = Expression.Parameter(typeof(object));
            var convExp = Expression.Convert(instExp, type);
            var fieldExp = Expression.Field(convExp, fieldInfo);
            return Expression.Lambda<Func<object, T>>(fieldExp, instExp).Compile();
        }
#endif

        private static ILog GetLogger()
        {
            return LogProvider.GetLogger(typeof(AspNetShutdownDetector));
        }
    }
}