// This file is part of Hangfire. Copyright © 2020 Sergey Odinokov.
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
using System.Reflection;
using System.Threading;
using Hangfire.Logging;

namespace Hangfire.Server
{
    internal static class AspNetShutdownDetector
    {
        private static readonly ILog Logger = LogProvider.GetLogger(typeof(AspNetShutdownDetector));
        private static readonly TimeSpan CheckForShutdownTimerInterval = TimeSpan.FromSeconds(1);
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

#if !NETSTANDARD1_3
        private static int _isInitialized;
        private static IDisposable _checkForShutdownTimer;
        private static Func<string> _shutdownReasonFunc;
        private static Func<bool> _checkConfigChangedFunc;
#endif

        public static CancellationToken GetShutdownToken()
        {
#if !NETSTANDARD1_3
            EnsureInitialized();
#endif
            return CancellationTokenSource.Token;
        }

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
                RegisterForStopListeningEvent();

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
                InitializeShutdownReason();

                // And the last method to check for application shutdown is implemented in
                // SignalR and Kudu service by checking the UnsafeIISMethods.MgdHasConfigChanged
                // method. But I was failed to find this method in the recent ASP.NET sources.
                // But nevertheless it may be useful to have it for older versions.
                InitializeMgdHasConfigChanged();

                _checkForShutdownTimer = new Timer(CheckForAppDomainShutdown, null, CheckForShutdownTimerInterval, CheckForShutdownTimerInterval);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Failed to initialize shutdown triggers for ASP.NET application.", ex);
            }
        }

        private static void CheckForAppDomainShutdown(object state)
        {
            if (_checkConfigChangedFunc != null && _checkConfigChangedFunc())
            {
                Cancel("UnsafeIISMethods.MgdHasConfigChanged");
            }

            var shutdownReason = _shutdownReasonFunc?.Invoke();
            if (shutdownReason != null)
            {
                Cancel($"HostingEnvironment.ShutdownReason: {shutdownReason}");
            }
        }

        private static void Cancel(string reason)
        {
            _checkForShutdownTimer?.Dispose();

            Logger.Info($"ASP.NET application is shutting down: {reason}.");

            try
            {
                CancellationTokenSource.Cancel(throwOnFirstException: false);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (AggregateException ag)
            {
                Logger.ErrorException("One or more exceptions were thrown during app pool shutdown: ", ag);
            }
        }

        private static void RegisterForStopListeningEvent()
        {
            try
            {
                var hostingEnvironmentType = Type.GetType("System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (hostingEnvironmentType == null) return;

                var stopEvent = hostingEnvironmentType.GetEvent("StopListening", BindingFlags.Static | BindingFlags.Public);
                if (stopEvent == null) return;

                stopEvent.AddEventHandler(null, new EventHandler(StopListening));
                Logger.Debug("HostingEnvironment.StopListening shutdown trigger initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.DebugException("Unable to initialize HostingEnvironment.StopListening shutdown trigger", ex);
            }
        }

        private static void StopListening(object sender, EventArgs e)
        {
            Cancel("HostingEnvironment.StopListening");
        }

        private static void InitializeShutdownReason()
        {
            try
            {
                var hostingEnvironment = Type.GetType("System.Web.Hosting.HostingEnvironment, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (hostingEnvironment == null) return;

                var shutdownReason = hostingEnvironment.GetProperty("ShutdownReason", BindingFlags.Static | BindingFlags.Public);
                if (shutdownReason == null) return;

                _shutdownReasonFunc = ShutdownReasonFunc;

                Logger.Debug("HostingEnvironment.ShutdownReason shutdown trigger initialized successfully.");

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
                    catch (Exception ex)
                    {
                        Logger.TraceException("Unable to call the HostingEnvironment.ShutdownReason property due to an exception.", ex);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.DebugException("Unable to initialize HostingEnvironment.ShutdownReason shutdown trigger", ex);
            }
        }

        private static void InitializeMgdHasConfigChanged()
        {
            try
            {
                var type = Type.GetType("System.Web.Hosting.UnsafeIISMethods, System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false);
                if (type == null) return;

                var methodInfo = type.GetMethod("MgdHasConfigChanged", BindingFlags.NonPublic  | BindingFlags.Static);
                if (methodInfo == null) return;

                _checkConfigChangedFunc = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), methodInfo);

                Logger.Debug("UnsafeIISMethods.MgdHasConfigChanged shutdown trigger initialized successfully.");
            }
            catch (Exception ex)
            {
                Logger.DebugException("Unable to initialize UnsafeIISMethods.MgdHasConfigChanged shutdown trigger", ex);
            }
        }
#endif
    }
}